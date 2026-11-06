using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Handlers;

namespace Win11ControlCenter.SystemEngine.Services;

public class OperationExecutor : IOperationExecutor
{
    private readonly ILoggingService _logger;
    private readonly BackupRepository _backupRepository;

    public OperationExecutor(ILoggingService logger, BackupRepository backupRepository)
    {
        _logger = logger;
        _backupRepository = backupRepository;
    }

    public Task<OperationPlan> GeneratePlanAsync(IEnumerable<OperationItem> requestedItems, string profileName, bool isDryRun)
    {
        var plan = new OperationPlan
        {
            Title = $"Optimization Plan - {profileName}",
            Description = $"Automated execution plan containing {requestedItems.Count()} operation(s).",
            ProfileName = profileName,
            IsDryRun = isDryRun,
            CreatedAt = DateTime.UtcNow,
            Status = ExecutionStatus.Planning
        };

        foreach (var item in requestedItems)
        {
            // STEP 2: ANALYZE
            var (isValid, reason) = SafetyValidator.ValidateOperation(item);
            if (!isValid)
            {
                item.Status = ExecutionStatus.Skipped;
                item.ResultMessage = $"Safety Block: {reason}";
                _logger.LogWarning($"Skipped item '{item.Name}': {reason}", "SafetyValidator");
            }
            else
            {
                item.Status = ExecutionStatus.Pending;
            }

            plan.Items.Add(item);
        }

        return Task.FromResult(plan);
    }

    public async Task<OperationPlan> ExecutePlanAsync(OperationPlan plan, IProgress<OperationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        plan.ExecutionStartTime = DateTime.UtcNow;
        plan.Status = ExecutionStatus.InProgress;

        _logger.LogInfo($"Beginning execution of plan '{plan.Title}' (IsDryRun={plan.IsDryRun}, Items={plan.Items.Count})", "OperationExecutor");

        // STEP 5: BACKUP
        if (!plan.IsDryRun)
        {
            try
            {
                plan.BackupDirectory = _backupRepository.CreateBackupSession(plan);
                _logger.LogInfo($"Backup created at: {plan.BackupDirectory}", "Backup");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create pre-execution backup", ex, "Backup");
            }
        }

        int index = 0;
        foreach (var item in plan.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;

            progress?.Report(new OperationProgress
            {
                CurrentIndex = index,
                TotalCount = plan.Items.Count,
                CurrentOperationName = item.Name,
                StatusMessage = $"Processing {item.Name}..."
            });

            if (item.Status == ExecutionStatus.Skipped)
            {
                _logger.LogOperation(item);
                continue;
            }

            item.Status = ExecutionStatus.InProgress;
            item.ExecutedAt = DateTime.UtcNow;

            // DRY RUN CHECK
            if (plan.IsDryRun)
            {
                item.Status = ExecutionStatus.Succeeded;
                item.ResultMessage = $"[DRY-RUN] Simulated modification verified: {item.Target} -> {item.RecommendedValue}";
                _logger.LogOperation(item);
                await Task.Delay(20, cancellationToken); // brief yield for UI responsiveness
                continue;
            }

            // STEP 7: EXECUTE & STEP 8: VERIFY
            var (success, message) = await ExecuteSingleItemAsync(item, cancellationToken);
            item.Status = success ? ExecutionStatus.Succeeded : ExecutionStatus.Failed;
            item.ResultMessage = message;
            item.BackupReference = plan.BackupDirectory;

            // STEP 9: LOG
            _logger.LogOperation(item);
            if (!success)
            {
                _logger.LogError($"Operation failed: {item.Name} -> {message}", null, "OperationExecutor");
            }
        }

        plan.ExecutionEndTime = DateTime.UtcNow;
        plan.Status = plan.OverallSuccess ? ExecutionStatus.Succeeded : ExecutionStatus.Failed;

        _logger.LogInfo($"Completed plan '{plan.Title}'. Succeeded={plan.SucceededCount}, Failed={plan.FailedCount}, Skipped={plan.SkippedCount}", "OperationExecutor");

        return plan;
    }

    public Task<bool> RollbackOperationAsync(OperationItem item, CancellationToken cancellationToken = default)
    {
        if (item.Reversibility == ReversibilityTier.Irreversible)
        {
            _logger.LogWarning($"Operation '{item.Name}' is irreversible and cannot be rolled back.", "Rollback");
            return Task.FromResult(false);
        }

        var (success, message) = RollbackSingleItem(item);
        if (success)
        {
            item.Status = ExecutionStatus.RolledBack;
            item.ResultMessage = $"Rolled back: {message}";
            _logger.LogOperation(item);
        }
        else
        {
            _logger.LogError($"Failed to rollback '{item.Name}': {message}", null, "Rollback");
        }

        return Task.FromResult(success);
    }

    public async Task<bool> RollbackPlanAsync(OperationPlan plan, IProgress<OperationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Rolling back plan '{plan.Title}'", "Rollback");
        int index = 0;
        bool allSuccess = true;

        // Rollback in reverse order
        var reversibleItems = plan.Items
            .Where(i => i.Status is ExecutionStatus.Succeeded or ExecutionStatus.Failed)
            .Reverse()
            .ToList();

        foreach (var item in reversibleItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;

            progress?.Report(new OperationProgress
            {
                CurrentIndex = index,
                TotalCount = reversibleItems.Count,
                CurrentOperationName = item.Name,
                StatusMessage = $"Rolling back {item.Name}..."
            });

            var ok = await RollbackOperationAsync(item, cancellationToken);
            if (!ok) allSuccess = false;
        }

        return allSuccess;
    }

    private async Task<(bool Success, string Message)> ExecuteSingleItemAsync(OperationItem item, CancellationToken cancellationToken)
    {
        return item.OperationType switch
        {
            OperationType.Registry => RegistryOperationHandler.Apply(item),
            OperationType.Service => ServiceOperationHandler.Apply(item),
            OperationType.Startup => StartupOperationHandler.Apply(item),
            OperationType.FileCleanup => WrapCleanupResult(CleanupOperationHandler.Apply(item)),
            OperationType.PackageRemoval => await PackageRemovalOperationHandler.ApplyAsync(item, cancellationToken),
            _ => (false, $"Unsupported operation type: {item.OperationType}")
        };
    }

    private (bool Success, string Message) RollbackSingleItem(OperationItem item)
    {
        return item.OperationType switch
        {
            OperationType.Registry => RegistryOperationHandler.Rollback(item),
            OperationType.Service => ServiceOperationHandler.Rollback(item),
            OperationType.Startup => StartupOperationHandler.Rollback(item),
            OperationType.FileCleanup => (false, "File cleanup operations are irreversible."),
            _ => (false, $"Unsupported operation type: {item.OperationType}")
        };
    }

    private static (bool Success, string Message) WrapCleanupResult((bool Success, string Message, long BytesDeleted, int FilesDeleted) res)
    {
        return (res.Success, res.Message);
    }
}

