using System.ComponentModel;
using System.Runtime.CompilerServices;
using Win11ControlCenter.Core.Enums;

namespace Win11ControlCenter.Core.Models;

public class CleanupTarget : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }
    
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

    [System.Text.Json.Serialization.JsonIgnore]
    public string FormattedTimestamp
    {
        get
        {
            if (DateTime.TryParse(CreatedAt, out var dt))
            {
                return dt.ToLocalTime().ToString("yyyy-MM-dd  HH:mm:ss");
            }

            if (!string.IsNullOrEmpty(BackupId) && BackupId.Length >= 19 && BackupId.Contains('_'))
            {
                var parts = BackupId.Split('_');
                if (parts.Length >= 2)
                {
                    var timeStr = parts[1].Replace('-', ':');
                    return $"{parts[0]}  {timeStr}";
                }
            }

            return BackupId;
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public string ShortId
    {
        get
        {
            if (string.IsNullOrEmpty(BackupId)) return string.Empty;
            var idx = BackupId.LastIndexOf('_');
            if (idx >= 0 && idx < BackupId.Length - 1)
            {
                return BackupId[(idx + 1)..];
            }
            return BackupId.Length > 8 ? BackupId[^6..] : BackupId;
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayTitle => !string.IsNullOrWhiteSpace(TriggerProfile)
        ? TriggerProfile
        : (!string.IsNullOrWhiteSpace(Description) ? Description : FormattedTimestamp);
}
