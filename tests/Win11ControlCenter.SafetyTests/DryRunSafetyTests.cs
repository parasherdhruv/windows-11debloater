using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.SafetyTests;

public class DryRunSafetyTests
{
    [Fact]
    public async Task DryRun_ExecutesWithoutMakingChanges()
    {
        var logger = new LoggingService();
        var backupRepo = new BackupRepository();
        var executor = new OperationExecutor(logger, backupRepo);

        var plan = new OperationPlan
        {
            Title = "Dry Run Test Plan",
            IsDryRun = true,
            Items = new List<OperationItem>
            {
                new()
                {
                    Name = "Sample Dry Run Tweak",
                    OperationType = OperationType.Registry,
                    Target = @"HKCU\Software\NonExistentTestKeyWin11CC",
                    RecommendedValue = 1
                }
            }
        };

        var executedPlan = await executor.ExecutePlanAsync(plan);

        Assert.Equal(ExecutionStatus.Succeeded, executedPlan.Status);
        Assert.Null(executedPlan.BackupDirectory); // Dry runs do not write backup snapshots to disk
        Assert.Contains("[DRY-RUN]", executedPlan.Items[0].ResultMessage);
    }
}

