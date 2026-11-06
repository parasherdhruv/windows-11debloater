using Win11ControlCenter.Core.Enums;

namespace Win11ControlCenter.Core.Models;

public class OperationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public OperationRiskLevel RiskLevel { get; set; } = OperationRiskLevel.Safe;
    public OperationType OperationType { get; set; } = OperationType.Registry;
    public ReversibilityTier Reversibility { get; set; } = ReversibilityTier.FullyReversible;
    
    public string Target { get; set; } = string.Empty;
    public object? OriginalValue { get; set; }
    public object? RecommendedValue { get; set; }
    public string ValueType { get; set; } = "DWord";
    
    public bool RequiresAdmin { get; set; }
    public bool RequiresRestart { get; set; }
    public bool RollbackSupported { get; set; } = true;
    
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;
    public string? ResultMessage { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? BackupReference { get; set; }
    
    public Dictionary<string, string> Metadata { get; set; } = new();

    public override string ToString() => $"[{OperationType}][{RiskLevel}] {Name} -> {Target}";
}

