using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.Core.Interfaces;

public interface IOperationExecutor
{
    Task<OperationPlan> GeneratePlanAsync(IEnumerable<OperationItem> requestedItems, string profileName, bool isDryRun);
    Task<OperationPlan> ExecutePlanAsync(OperationPlan plan, IProgress<OperationProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<bool> RollbackOperationAsync(OperationItem item, CancellationToken cancellationToken = default);
    Task<bool> RollbackPlanAsync(OperationPlan plan, IProgress<OperationProgress>? progress = null, CancellationToken cancellationToken = default);
}

public class OperationProgress
{
    public int CurrentIndex { get; set; }
    public int TotalCount { get; set; }
    public string CurrentOperationName { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public double PercentComplete => TotalCount == 0 ? 0 : (CurrentIndex / (double)TotalCount) * 100.0;
}

