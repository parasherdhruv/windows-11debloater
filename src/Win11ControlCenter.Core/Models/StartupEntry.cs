using Win11ControlCenter.Core.Enums;

namespace Win11ControlCenter.Core.Models;

public class StartupEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string Publisher { get; set; } = "Unknown";
    
    public StartupLocationType LocationType { get; set; } = StartupLocationType.RegistryCurrentUser;
    public string RegistryKeyPath { get; set; } = string.Empty;
    public string LocationText => LocationType switch
    {
        StartupLocationType.RegistryCurrentUser => "User Registry",
        StartupLocationType.RegistryLocalMachine => "System Registry",
        StartupLocationType.UserStartupFolder => "Startup Folder",
        StartupLocationType.CommonStartupFolder => "Common Startup",
        _ => LocationType.ToString()
    };
    
    public bool IsEnabled { get; set; } = true;
    public string StatusText => IsEnabled ? "Enabled" : "Disabled";
    public bool ExistsOnDisk { get; set; } = true;
    public string DiskStatusText => ExistsOnDisk ? "Verified on Disk" : "File Missing";
    public bool IsSuspicious { get; set; }
    public OperationRiskLevel RiskLevel { get; set; } = OperationRiskLevel.Safe;
}

