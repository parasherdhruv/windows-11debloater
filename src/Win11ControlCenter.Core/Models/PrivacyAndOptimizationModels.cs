using Win11ControlCenter.Core.Enums;

namespace Win11ControlCenter.Core.Models;

public class PrivacyTweak
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Telemetry";
    
    public string RegistryHive { get; set; } = "HKCU"; // HKCU or HKLM
    public string KeyPath { get; set; } = string.Empty;
    public string ValueName { get; set; } = string.Empty;
    public string ValueType { get; set; } = "DWord";
    
    public object? CurrentValue { get; set; }
    public object? RecommendedValue { get; set; }
    public object? OriginalValue { get; set; }
    public object? DefaultValue { get; set; }
    
    public bool RequiresAdmin => RegistryHive.Equals("HKLM", StringComparison.OrdinalIgnoreCase);
    public bool RequiresRestart { get; set; }
    public bool IsApplied { get; set; }
    public OperationRiskLevel RiskLevel { get; set; } = OperationRiskLevel.Safe;
    public string ScopeDescription => RequiresAdmin ? "Machine-wide (HKLM)" : "Current User (HKCU)";
}

public class OptimizationTweak
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Explorer";
    
    public string RegistryHive { get; set; } = "HKCU";
    public string KeyPath { get; set; } = string.Empty;
    public string ValueName { get; set; } = string.Empty;
    public string ValueType { get; set; } = "DWord";
    
    public object? CurrentValue { get; set; }
    public object? RecommendedValue { get; set; }
    public object? OriginalValue { get; set; }
    public object? DefaultValue { get; set; }
    
    public bool RequiresAdmin => RegistryHive.Equals("HKLM", StringComparison.OrdinalIgnoreCase);
    public bool RequiresRestart { get; set; }
    public bool IsApplied { get; set; }
    public OperationRiskLevel RiskLevel { get; set; } = OperationRiskLevel.Safe;
    public string ScopeDescription => RequiresAdmin ? "Machine-wide (HKLM)" : "Current User (HKCU)";
}

public class PowerPlanInfo
{
    public string Guid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class OptimizationProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "⚡";
    public string SafetyTier { get; set; } = "Safe / High Reversibility";
    public string ExpectedImpact { get; set; } = string.Empty;
    public string RequiredPermissions { get; set; } = "Standard User";
    public string EstimatedReversibility { get; set; } = "Fully Reversible via Restore Center";
    public bool IsCustom { get; set; }

    public List<string> PrivacyTweakIds { get; set; } = new();
    public List<string> OptimizationTweakIds { get; set; } = new();
    public Dictionary<string, string> ServiceAdjustments { get; set; } = new(); // ServiceName -> Desired StartupType (e.g. "Manual")
    public string? TargetPowerPlanName { get; set; }
}

public class ProfileDiffItem
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string TargetValue { get; set; } = string.Empty;
    public bool WillChange { get; set; }
    public OperationRiskLevel RiskLevel { get; set; } = OperationRiskLevel.Safe;
    public string Scope { get; set; } = "User";
}

public class ProfileDiffResult
{
    public string ProfileId { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public int TotalItems => Items.Count;
    public int ItemsToChange => Items.Count(i => i.WillChange);
    public int ItemsAlreadyCompliant => Items.Count(i => !i.WillChange);
    public List<ProfileDiffItem> Items { get; set; } = new();
}
