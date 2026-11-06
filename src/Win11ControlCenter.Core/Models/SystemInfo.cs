namespace Win11ControlCenter.Core.Models;

public class SystemInfo
{
    public string OsName { get; set; } = "Windows 11";
    public string OsVersion { get; set; } = string.Empty;
    public string OsBuild { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public string Architecture { get; set; } = "x64";
    public string CpuModel { get; set; } = string.Empty;
    public string GpuModel { get; set; } = string.Empty;
    
    public double TotalRamGigabytes { get; set; }
    public double AvailableRamGigabytes { get; set; }
    public double CpuUsagePercent { get; set; }
    
    public double SystemDriveTotalGigabytes { get; set; }
    public double SystemDriveFreeGigabytes { get; set; }
    
    public string ActivePowerPlan { get; set; } = "Balanced";
    public bool IsElevated { get; set; }
    public string ElevationStatusText => IsElevated ? "Administrator (Elevated)" : "Standard User (Least Privilege)";
    public bool IsDomainJoined { get; set; }
    public bool IsPendingRestart { get; set; }
    public bool IsSystemRestoreEnabled { get; set; }
    public string WindowsActivationStatus { get; set; } = "Unknown";
    
    public int InstalledAppCount { get; set; }
    public int ProvisionedAppCount { get; set; }
    public int StartupAppCount { get; set; }
    public int RunningServiceCount { get; set; }
    public int DisabledServiceCount { get; set; }
    public long MeasurableTempBytes { get; set; }
    
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}
