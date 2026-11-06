using System.ServiceProcess;
using Microsoft.Win32;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Win11ControlCenter.SystemEngine.Handlers;

namespace Win11ControlCenter.SystemEngine.Services;

public class ServiceManagerService : IServiceManagerService
{
    private readonly ILoggingService _logger;

    public ServiceManagerService(ILoggingService logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<WindowsServiceItem>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var items = new List<WindowsServiceItem>();

            try
            {
                var services = ServiceController.GetServices();
                foreach (var sc in services)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var name = sc.ServiceName;
                        var isCore = CoreWindowsServices.IsProtectedService(name);
                        var (desc, startType, binaryPath, account) = ReadServiceRegistryDetails(name);

                        var category = isCore ? ServiceCategory.CoreWindows : CategorizeService(name, sc.DisplayName);
                        var risk = isCore ? OperationRiskLevel.Critical : CalculateServiceRisk(category);

                        var item = new WindowsServiceItem
                        {
                            ServiceName = name,
                            DisplayName = string.IsNullOrWhiteSpace(sc.DisplayName) ? name : sc.DisplayName,
                            Description = desc,
                            Status = sc.Status.ToString(),
                            StartupType = startType,
                            OriginalStartupType = startType,
                            BinaryPath = binaryPath,
                            ServiceAccount = account,
                            Category = category,
                            RiskLevel = risk,
                            IsCoreService = isCore
                        };

                        try
                        {
                            foreach (var dep in sc.ServicesDependedOn)
                            {
                                item.Dependencies.Add(dep.ServiceName);
                            }
                        }
                        catch { }

                        try
                        {
                            foreach (var dep in sc.DependentServices)
                            {
                                item.DependentServices.Add(dep.ServiceName);
                            }
                        }
                        catch { }

                        items.Add(item);
                    }
                    catch
                    {
                        // Ignore individual access exceptions
                    }
                }

                _logger.LogInfo($"Services discovery: {items.Count} services cataloged.", "ServicesManager");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed during service enumeration", ex, "ServicesManager");
            }

            return (IReadOnlyList<WindowsServiceItem>)items.OrderBy(s => s.DisplayName).ToList();
        }, cancellationToken);
    }

    public Task<bool> SetStartupTypeAsync(string serviceName, string startupType, CancellationToken cancellationToken = default)
    {
        if (CoreWindowsServices.IsProtectedService(serviceName))
        {
            _logger.LogWarning($"Safety shield blocked modification of protected service: {serviceName}", "ServicesManager");
            return Task.FromResult(false);
        }

        var success = ServiceOperationHandler.SetStartupType(serviceName, startupType, out var error);
        if (success)
        {
            _logger.LogInfo($"Configured service '{serviceName}' startup type to '{startupType}'.", "ServicesManager");
        }
        else
        {
            _logger.LogError($"Failed to set service '{serviceName}' startup type: {error}", null, "ServicesManager");
        }

        return Task.FromResult(success);
    }

    public Task<bool> StartServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                    _logger.LogInfo($"Started service '{serviceName}'.", "ServicesManager");
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start service '{serviceName}'", ex, "ServicesManager");
                return false;
            }
        }, cancellationToken);
    }

    public Task<bool> StopServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (CoreWindowsServices.IsProtectedService(serviceName))
        {
            _logger.LogWarning($"Safety shield blocked stop command for protected service: {serviceName}", "ServicesManager");
            return Task.FromResult(false);
        }

        return Task.Run(() =>
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Stopped && sc.CanStop)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                    _logger.LogInfo($"Stopped service '{serviceName}'.", "ServicesManager");
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to stop service '{serviceName}'", ex, "ServicesManager");
                return false;
            }
        }, cancellationToken);
    }

    public async Task<string> ExportServicesInventoryAsync(IEnumerable<WindowsServiceItem> services, string destinationPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            var json = System.Text.Json.JsonSerializer.Serialize(services, options);
            var targetDir = System.IO.Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(targetDir))
            {
                System.IO.Directory.CreateDirectory(targetDir);
            }

            await System.IO.File.WriteAllTextAsync(destinationPath, json, cancellationToken);
            _logger.LogInfo($"Exported services inventory to '{destinationPath}'.", "ServicesManager");
            return destinationPath;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to export services inventory", ex, "ServicesManager");
            throw;
        }
    }

    private static (string Description, string StartupType, string BinaryPath, string Account) ReadServiceRegistryDetails(string serviceName)
    {
        string desc = string.Empty;
        string startupType = "Manual";
        string binaryPath = string.Empty;
        string account = "LocalSystem";

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@$"SYSTEM\CurrentControlSet\Services\{serviceName}", false);
            if (key != null)
            {
                desc = key.GetValue("Description")?.ToString() ?? string.Empty;
                binaryPath = key.GetValue("ImagePath")?.ToString() ?? string.Empty;
                account = key.GetValue("ObjectName")?.ToString() ?? "LocalSystem";

                var startVal = key.GetValue("Start");
                if (startVal is int val)
                {
                    startupType = val switch
                    {
                        2 => "Automatic",
                        3 => "Manual",
                        4 => "Disabled",
                        _ => "Manual"
                    };
                }
            }
        }
        catch { }

        return (desc, startupType, binaryPath, account);
    }

    private static ServiceCategory CategorizeService(string name, string displayName)
    {
        var combined = $"{name} {displayName}".ToLowerInvariant();

        if (combined.Contains("telemetry") || combined.Contains("diagnostics") || combined.Contains("feedback") || combined.Contains("diagtrack"))
            return ServiceCategory.Telemetry;
        if (combined.Contains("audio") || combined.Contains("sound"))
            return ServiceCategory.Audio;
        if (combined.Contains("bluetooth") || combined.Contains("bth"))
            return ServiceCategory.Bluetooth;
        if (combined.Contains("print") || combined.Contains("spooler"))
            return ServiceCategory.Printing;
        if (combined.Contains("xbox") || combined.Contains("game"))
            return ServiceCategory.Gaming;
        if (combined.Contains("nvidia") || combined.Contains("amd") || combined.Contains("intel") || combined.Contains("realtek"))
            return ServiceCategory.OEM;

        return ServiceCategory.ThirdParty;
    }

    private static OperationRiskLevel CalculateServiceRisk(ServiceCategory category)
    {
        return category switch
        {
            ServiceCategory.CoreWindows or ServiceCategory.Security => OperationRiskLevel.Critical,
            ServiceCategory.Networking or ServiceCategory.Storage or ServiceCategory.Audio => OperationRiskLevel.Advanced,
            ServiceCategory.Printing or ServiceCategory.Bluetooth => OperationRiskLevel.Moderate,
            ServiceCategory.Telemetry or ServiceCategory.Gaming or ServiceCategory.ThirdParty => OperationRiskLevel.Safe,
            _ => OperationRiskLevel.Moderate
        };
    }
}

