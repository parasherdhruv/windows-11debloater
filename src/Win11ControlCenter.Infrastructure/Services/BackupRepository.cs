using System.Text.Json;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.Infrastructure.Services;

public class BackupRepository
{
    private readonly string _backupsRoot;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public BackupRepository()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _backupsRoot = Path.Combine(localAppData, "Win11ControlCenter", "Backups");
        Directory.CreateDirectory(_backupsRoot);
    }

    public string CreateBackupSession(OperationPlan plan, string windowsBuild = "")
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var sessionDir = Path.Combine(_backupsRoot, timestamp);
        if (Directory.Exists(sessionDir))
        {
            sessionDir = Path.Combine(_backupsRoot, $"{timestamp}_{Guid.NewGuid().ToString("N")[..4]}");
        }
        Directory.CreateDirectory(sessionDir);

        var regDir = Path.Combine(sessionDir, "registry");
        Directory.CreateDirectory(regDir);

        var backupId = Path.GetFileName(sessionDir);

        var metadata = new BackupMetadata
        {
            BackupId = backupId,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            Description = $"Backup for plan: {plan.Title}",
            TriggerProfile = plan.ProfileName,
            WindowsBuild = windowsBuild,
            Path = sessionDir,
            RegistryEntriesCount = plan.Items.Count(i => i.OperationType == Core.Enums.OperationType.Registry),
            ServicesCount = plan.Items.Count(i => i.OperationType == Core.Enums.OperationType.Service),
            StartupCount = plan.Items.Count(i => i.OperationType == Core.Enums.OperationType.Startup),
            PackageCount = plan.Items.Count(i => i.OperationType == Core.Enums.OperationType.PackageRemoval)
        };

        File.WriteAllText(Path.Combine(sessionDir, "metadata.json"), JsonSerializer.Serialize(metadata, JsonOptions));
        File.WriteAllText(Path.Combine(sessionDir, "operations.json"), JsonSerializer.Serialize(plan, JsonOptions));

        GenerateEmergencyRegFile(plan, Path.Combine(regDir, "pre_apply_rollback.reg"));

        return sessionDir;
    }

    public IReadOnlyList<BackupMetadata> GetAllBackups()
    {
        var results = new List<BackupMetadata>();
        if (!Directory.Exists(_backupsRoot)) return results;

        foreach (var dir in Directory.GetDirectories(_backupsRoot))
        {
            var metaFile = Path.Combine(dir, "metadata.json");
            if (File.Exists(metaFile))
            {
                try
                {
                    var meta = JsonSerializer.Deserialize<BackupMetadata>(File.ReadAllText(metaFile));
                    if (meta != null)
                    {
                        meta.Path = dir;
                        results.Add(meta);
                    }
                }
                catch
                {
                    // Ignore corrupted directory
                }
            }
        }

        return results.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public OperationPlan? LoadBackupPlan(string backupPath)
    {
        var opFile = Path.Combine(backupPath, "operations.json");
        if (!File.Exists(opFile)) return null;

        try
        {
            return JsonSerializer.Deserialize<OperationPlan>(File.ReadAllText(opFile));
        }
        catch
        {
            return null;
        }
    }

    public bool DeleteBackupSession(string backupId)
    {
        try
        {
            var sessionDir = Path.Combine(_backupsRoot, backupId);
            if (!Directory.Exists(sessionDir)) return false;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Directory.Delete(sessionDir, true);
                    return true;
                }
                catch (IOException)
                {
                    Thread.Sleep(50);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(50);
                }
            }

            return !Directory.Exists(sessionDir);
        }
        catch
        {
            return false;
        }
    }

    public string GetBackupsRoot() => _backupsRoot;

    private void GenerateEmergencyRegFile(OperationPlan plan, string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var regLines = new List<string>
        {
            "Windows Registry Editor Version 5.00",
            "",
            $"; Emergency Rollback File for Win11 Control Center Plan: {plan.Title}",
            $"; Generated: {DateTime.Now}",
            ""
        };

        var regItems = plan.Items.Where(i => i.OperationType == Core.Enums.OperationType.Registry);
        var groupedByPath = regItems.GroupBy(i => i.Target);

        foreach (var group in groupedByPath)
        {
            regLines.Add($"[{group.Key}]");
            foreach (var item in group)
            {
                if (item.Metadata.TryGetValue("ValueName", out var valName))
                {
                    if (item.OriginalValue == null)
                    {
                        // Remove value
                        regLines.Add($"\"{valName}\"=-");
                    }
                    else if (item.OriginalValue is int or long)
                    {
                        regLines.Add($"\"{valName}\"=dword:{Convert.ToInt32(item.OriginalValue):x8}");
                    }
                    else
                    {
                        regLines.Add($"\"{valName}\"=\"{item.OriginalValue}\"");
                    }
                }
            }
            regLines.Add("");
        }

        File.WriteAllLines(filePath, regLines);
    }
}

