using Win11ControlCenter.Core.Enums;

namespace Win11ControlCenter.Core.Models;

public class ApplicationPackage
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PackageFullName { get; set; } = string.Empty;
    public string PackageFamilyName { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    
    public string PackageType { get; set; } = "AppX"; // AppX, MSIX, Win32, Store
    public string InstallScope { get; set; } = "CurrentUser"; // CurrentUser, AllUsers, Provisioned
    public bool IsProvisioned { get; set; }
    public bool IsInstalledForCurrentUser { get; set; } = true;
    
    public PackageCategory Category { get; set; } = PackageCategory.Unknown;
    public OperationRiskLevel RiskLevel { get; set; } = OperationRiskLevel.Moderate;
    public bool IsProtected { get; set; }
    public bool IsRemovable => !IsProtected;
    
    public long? EstimatedSizeBytes { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? InstallLocation { get; set; }
    
    public bool IsSelectedForRemoval { get; set; }
    public bool RemoveProvisioningToo { get; set; } = true;

    public string SizeFormatted
    {
        get
        {
            if (!EstimatedSizeBytes.HasValue || EstimatedSizeBytes.Value <= 0) return "N/A";
            double mb = EstimatedSizeBytes.Value / (1024.0 * 1024.0);
            return mb < 1024 ? $"{mb:F1} MB" : $"{mb / 1024.0:F2} GB";
        }
    }
}

