using Win11ControlCenter.Core.Enums;

namespace Win11ControlCenter.Core.Models;

public class WindowsServiceItem
{
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public string Status { get; set; } = "Stopped"; // Running, Stopped, Paused
    public string StartupType { get; set; } = "Manual"; // Automatic, Manual, Disabled, Delayed
    public string OriginalStartupType { get; set; } = "Manual";
    
    public string BinaryPath { get; set; } = string.Empty;
    public string ServiceAccount { get; set; } = "LocalSystem";
    
    public ServiceCategory Category { get; set; } = ServiceCategory.CoreWindows;
    public OperationRiskLevel RiskLevel { get; set; } = OperationRiskLevel.Critical;
    public bool IsCoreService { get; set; }
    public bool IsMicrosoftSigned { get; set; } = true;
    
    public List<string> Dependencies { get; set; } = new();
    public List<string> DependentServices { get; set; } = new();
    
    public bool CanBeModified => !IsCoreService;
}

