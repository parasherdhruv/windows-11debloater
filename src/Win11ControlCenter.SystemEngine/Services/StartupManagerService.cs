using System.IO;
using Microsoft.Win32;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.SystemEngine.Handlers;

namespace Win11ControlCenter.SystemEngine.Services;

public class StartupManagerService : IStartupManagerService
{
    private readonly ILoggingService _logger;

    public StartupManagerService(ILoggingService logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<StartupEntry>> GetStartupEntriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var entries = new List<StartupEntry>();

            try
            {
                // 1. Current User Run Key
                ReadRegistryRunEntries(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", StartupLocationType.RegistryCurrentUser, entries);

                // 2. Local Machine Run Key
                ReadRegistryRunEntries(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", StartupLocationType.RegistryLocalMachine, entries);

                // 3. User Startup Folder
                var userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                ReadStartupFolderEntries(userStartup, StartupLocationType.UserStartupFolder, entries);

                // 4. Common Startup Folder
                var commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
                ReadStartupFolderEntries(commonStartup, StartupLocationType.CommonStartupFolder, entries);

                // 5. Evaluate enabled/disabled state via StartupApproved
                EvaluateStartupApprovedStatus(entries);

                _logger.LogInfo($"Startup discovery complete: {entries.Count} startup item(s) found.", "StartupManager");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed during startup item discovery", ex, "StartupManager");
            }

            return (IReadOnlyList<StartupEntry>)entries;
        }, cancellationToken);
    }

    public Task<bool> SetStartupEntryStateAsync(StartupEntry entry, bool enable, CancellationToken cancellationToken = default)
    {
        var item = new OperationItem
        {
            Name = $"{(enable ? "Enable" : "Disable")} startup {entry.Name}",
            OperationType = OperationType.Startup,
            Target = entry.Name,
            RecommendedValue = enable
        };

        var (success, message) = StartupOperationHandler.Apply(item);
        if (success)
        {
            entry.IsEnabled = enable;
            _logger.LogInfo(message, "StartupManager");
        }
        else
        {
            _logger.LogError(message, null, "StartupManager");
        }

        return Task.FromResult(success);
    }

    public void OpenFileLocation(StartupEntry entry)
    {
        try
        {
            if (File.Exists(entry.ExecutablePath))
            {
                var psi = new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{entry.ExecutablePath}\"")
                {
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Could not open file location for '{entry.Name}'", ex, "StartupManager");
        }
    }

    public async Task<string> ExportStartupInventoryAsync(IEnumerable<StartupEntry> entries, string destinationPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            var json = System.Text.Json.JsonSerializer.Serialize(entries, options);
            var targetDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            await File.WriteAllTextAsync(destinationPath, json, cancellationToken);
            _logger.LogInfo($"Exported startup inventory to '{destinationPath}'.", "StartupManager");
            return destinationPath;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to export startup inventory", ex, "StartupManager");
            throw;
        }
    }

    private static void ReadRegistryRunEntries(RegistryKey rootHive, string subKeyPath, StartupLocationType locationType, List<StartupEntry> results)
    {
        try
        {
            using var key = rootHive.OpenSubKey(subKeyPath, false);
            if (key == null) return;

            foreach (var valName in key.GetValueNames())
            {
                if (string.IsNullOrWhiteSpace(valName) || 
                    string.Equals(valName, "desktop", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(valName, "desktop.ini", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var command = key.GetValue(valName)?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(command)) continue;

                var exePath = ExtractExecutablePath(command);
                var exists = !string.IsNullOrEmpty(exePath) && File.Exists(exePath);

                results.Add(new StartupEntry
                {
                    Name = valName,
                    Command = command,
                    ExecutablePath = exePath,
                    LocationType = locationType,
                    RegistryKeyPath = $"{rootHive.Name}\\{subKeyPath}",
                    ExistsOnDisk = exists,
                    IsSuspicious = !exists,
                    Publisher = "Registry Startup Entry",
                    IsEnabled = true
                });
            }
        }
        catch { }
    }

    private static void ReadStartupFolderEntries(string folderPath, StartupLocationType locationType, List<StartupEntry> results)
    {
        try
        {
            if (!Directory.Exists(folderPath)) return;

            foreach (var file in Directory.GetFiles(folderPath))
            {
                var fileName = Path.GetFileName(file);
                if (string.Equals(fileName, "desktop.ini", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(name)) continue;

                results.Add(new StartupEntry
                {
                    Name = name,
                    Command = file,
                    ExecutablePath = file,
                    LocationType = locationType,
                    ExistsOnDisk = true,
                    IsSuspicious = false,
                    Publisher = "Startup Shortcut",
                    IsEnabled = true
                });
            }
        }
        catch { }
    }

    private static void EvaluateStartupApprovedStatus(List<StartupEntry> entries)
    {
        try
        {
            const string approvedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
            using var key = Registry.CurrentUser.OpenSubKey(approvedKeyPath, false);
            if (key == null) return;

            foreach (var entry in entries.Where(e => e.LocationType == StartupLocationType.RegistryCurrentUser))
            {
                var raw = key.GetValue(entry.Name);
                if (raw is byte[] bytes && bytes.Length > 0)
                {
                    // Byte 0: 0x02 = Enabled, 0x03 = Disabled
                    entry.IsEnabled = (bytes[0] % 2 == 0);
                }
            }
        }
        catch { }
    }

    private static string ExtractExecutablePath(string rawCommand)
    {
        if (string.IsNullOrWhiteSpace(rawCommand)) return string.Empty;

        var trimmed = rawCommand.Trim();
        if (trimmed.StartsWith("\""))
        {
            var closingQuote = trimmed.IndexOf('\"', 1);
            if (closingQuote > 1)
            {
                return trimmed.Substring(1, closingQuote - 1);
            }
        }

        var spaceIdx = trimmed.IndexOf(' ');
        return spaceIdx > 0 ? trimmed.Substring(0, spaceIdx) : trimmed;
    }
}

