using Win11ControlCenter.Core.Enums;

namespace Win11ControlCenter.Core.Models;

public class CleanupTarget
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public bool IsSelected { get; set; } = true;
    public OperationRiskLevel RiskLevel { get; set; } = OperationRiskLevel.Safe;
    public bool RequiresAdmin { get; set; }
    
    public List<string> ExcludedPatterns { get; set; } = new();

    public string SizeFormatted
    {
        get
        {
            if (TotalSizeBytes <= 0) return "0 MB";
            double mb = TotalSizeBytes / (1024.0 * 1024.0);
            return mb < 1024 ? $"{mb:F1} MB" : $"{mb / 1024.0:F2} GB";
        }
    }
}

public class BackupMetadata
{
    public string BackupId { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TriggerProfile { get; set; } = string.Empty;
    public string WindowsBuild { get; set; } = string.Empty;
    public int RegistryEntriesCount { get; set; }
    public int ServicesCount { get; set; }
    public int StartupCount { get; set; }
    public int PackageCount { get; set; }
    public string Path { get; set; } = string.Empty;
}

