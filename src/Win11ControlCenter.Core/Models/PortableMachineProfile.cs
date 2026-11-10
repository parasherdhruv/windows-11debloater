using Win11ControlCenter.Core.Enums;

namespace Win11ControlCenter.Core.Models;

/// <summary>
/// Portable, self-contained system configuration profile (.w11profile).
/// Enables cross-machine deployment, backup, and sharing of optimization configurations.
/// </summary>
public class PortableMachineProfile
{
    public string SchemaVersion { get; set; } = "1.0.0";
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Custom Machine Profile";
    public string Description { get; set; } = "Portable Windows 11 optimization profile.";
    public string Author { get; set; } = Environment.UserName;
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
    public string MinimumOsBuild { get; set; } = "Windows 11 (22H2 / 23H2 / 24H2)";
    public string Icon { get; set; } = "⚡";
    public string SafetyTier { get; set; } = "Safe / High Reversibility";
    public bool CreateRestorePointOnApply { get; set; } = true;

    // 1. App Debloater target packages to remove
    public List<string> DebloatPackageNames { get; set; } = new();

    // 2. Privacy & Telemetry tweak identifiers
    public List<string> PrivacyTweakIds { get; set; } = new();

    // 3. Windows Explorer & UI tweak identifiers
    public List<string> OptimizationTweakIds { get; set; } = new();

    // 4. Windows Services adjustments (ServiceName -> Desired StartupType: "Manual", "Disabled", "Automatic")
    public Dictionary<string, string> ServiceAdjustments { get; set; } = new();

    // 5. Startup applications to disable
    public List<string> DisabledStartupItems { get; set; } = new();

    // 6. System Cleaner categories to purge
    public List<string> CleanerCategories { get; set; } = new();

    // 7. Target Windows Power Scheme (e.g. "High performance", "Ultimate Performance", "Balanced")
    public string? TargetPowerPlanName { get; set; }

    /// <summary>
    /// Total count of all configuration operations defined in this profile.
    /// </summary>
    public int TotalActionCount =>
        DebloatPackageNames.Count +
        PrivacyTweakIds.Count +
        OptimizationTweakIds.Count +
        ServiceAdjustments.Count +
        DisabledStartupItems.Count +
        CleanerCategories.Count +
        (string.IsNullOrWhiteSpace(TargetPowerPlanName) ? 0 : 1);

    /// <summary>
    /// Creates a portable machine profile from an in-app OptimizationProfile.
    /// </summary>
    public static PortableMachineProfile FromOptimizationProfile(OptimizationProfile profile, string author = "")
    {
        return new PortableMachineProfile
        {
            Id = string.IsNullOrWhiteSpace(profile.Id) ? Guid.NewGuid().ToString("N") : profile.Id,
            Name = profile.Name,
            Description = profile.Description,
            Icon = profile.Icon,
            SafetyTier = profile.SafetyTier,
            Author = string.IsNullOrWhiteSpace(author) ? Environment.UserName : author,
            ExportedAtUtc = DateTime.UtcNow,
            PrivacyTweakIds = new List<string>(profile.PrivacyTweakIds),
            OptimizationTweakIds = new List<string>(profile.OptimizationTweakIds),
            ServiceAdjustments = new Dictionary<string, string>(profile.ServiceAdjustments),
            TargetPowerPlanName = profile.TargetPowerPlanName
        };
    }

    /// <summary>
    /// Converts a portable machine profile into an in-app OptimizationProfile.
    /// </summary>
    public OptimizationProfile ToOptimizationProfile()
    {
        return new OptimizationProfile
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Icon = Icon,
            SafetyTier = SafetyTier,
            ExpectedImpact = $"Imported profile with {TotalActionCount} configured adjustments.",
            RequiredPermissions = "Standard User / Admin for Machine Tweaks",
            EstimatedReversibility = "Fully Reversible via Restore Center",
            IsCustom = true,
            PrivacyTweakIds = new List<string>(PrivacyTweakIds),
            OptimizationTweakIds = new List<string>(OptimizationTweakIds),
            ServiceAdjustments = new Dictionary<string, string>(ServiceAdjustments),
            TargetPowerPlanName = TargetPowerPlanName
        };
    }
}
