using Win11ControlCenter.Core.Enums;

namespace Win11ControlCenter.Core.Models;

public class OperationPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProfileName { get; set; } = "Custom";
    public bool IsDryRun { get; set; }
    public bool UserConsentGranted { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutionStartTime { get; set; }
    public DateTime? ExecutionEndTime { get; set; }
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Planning;
    
    public List<OperationItem> Items { get; set; } = new();
    
    public string? BackupDirectory { get; set; }
    public string? RestorePointName { get; set; }
    public bool OverallSuccess => Items.Count > 0 && Items.All(i => i.Status is ExecutionStatus.Succeeded or ExecutionStatus.Skipped);
    
    public int TotalItems => Items.Count;
    public int SucceededCount => Items.Count(i => i.Status == ExecutionStatus.Succeeded);
    public int FailedCount => Items.Count(i => i.Status == ExecutionStatus.Failed);
    public int SkippedCount => Items.Count(i => i.Status == ExecutionStatus.Skipped);

    public OperationRiskLevel HighestRiskLevel => Items.Count == 0 ? OperationRiskLevel.None : Items.Max(i => i.RiskLevel);
}

