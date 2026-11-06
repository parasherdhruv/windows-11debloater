using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;

namespace Win11ControlCenter.SystemEngine.Handlers;

public static class ServiceOperationHandler
{
    public static (bool Success, string Message) Apply(OperationItem item)
    {
        if (CoreWindowsServices.IsProtectedService(item.Target))
        {
            return (false, $"Safety block: Service '{item.Target}' is a core Windows infrastructure component and cannot be modified.");
        }

        try
        {
            var requestedType = item.RecommendedValue?.ToString() ?? "Manual";
            var success = SetStartupType(item.Target, requestedType, out var error);
            if (!success)
            {
                return (false, $"Failed to set startup type for service '{item.Target}': {error}");
            }

            return (true, $"Set service '{item.Target}' startup type to '{requestedType}'.");
        }
        catch (Exception ex)
        {
            return (false, $"Service error: {ex.Message}");
        }
    }

    public static (bool Success, string Message) Rollback(OperationItem item)
    {
        if (CoreWindowsServices.IsProtectedService(item.Target))
        {
            return (false, $"Safety block: Service '{item.Target}' is a core Windows service.");
        }

        try
        {
            var originalType = item.OriginalValue?.ToString() ?? "Manual";
            var success = SetStartupType(item.Target, originalType, out var error);
            if (!success)
            {
                return (false, $"Failed to rollback service '{item.Target}': {error}");
            }

            return (true, $"Rolled back service '{item.Target}' to original startup type: '{originalType}'.");
        }
        catch (Exception ex)
        {
            return (false, $"Service rollback error: {ex.Message}");
        }
    }

    public static bool SetStartupType(string serviceName, string startupType, out string error)
    {
        error = string.Empty;
        if (CoreWindowsServices.IsProtectedService(serviceName))
        {
            error = "Service is protected.";
            return false;
        }

        try
        {
            // Map type to registry Start value: 2=Auto, 3=Manual, 4=Disabled
            int startVal = startupType.ToLowerInvariant() switch
            {
                "automatic" or "auto" => 2,
                "manual" or "demand" => 3,
                "disabled" => 4,
                _ => 3
            };

            using var key = Registry.LocalMachine.OpenSubKey(@$"SYSTEM\CurrentControlSet\Services\{serviceName}", true);
            if (key != null)
            {
                key.SetValue("Start", startVal, RegistryValueKind.DWord);
                return true;
            }

            // Fallback via sc.exe config
            var scType = startupType.ToLowerInvariant() switch
            {
                "automatic" or "auto" => "auto",
                "manual" or "demand" => "demand",
                "disabled" => "disabled",
                _ => "demand"
            };

            var psi = new ProcessStartInfo("sc.exe", $"config \"{serviceName}\" start= {scType}")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
            return proc?.ExitCode == 0;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static string GetCurrentStartupType(string serviceName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@$"SYSTEM\CurrentControlSet\Services\{serviceName}", false);
            if (key != null)
            {
                var val = key.GetValue("Start");
                if (val is int intVal)
                {
                    return intVal switch
                    {
                        2 => "Automatic",
                        3 => "Manual",
                        4 => "Disabled",
                        _ => "Manual"
                    };
                }
            }
        }
        catch
        {
            // Ignore
        }

        return "Manual";
    }
}

