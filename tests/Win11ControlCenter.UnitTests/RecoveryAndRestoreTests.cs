using System.IO;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class RecoveryAndRestoreTests
{
    private readonly LoggingService _logger;
    private readonly BackupRepository _backupRepo;
    private readonly OperationExecutor _opExecutor;
    private readonly RestoreService _restoreService;

    public RecoveryAndRestoreTests()
    {
        _logger = new LoggingService();
        _backupRepo = new BackupRepository();
        _opExecutor = new OperationExecutor(_logger, _backupRepo);
        _restoreService = new RestoreService(_logger, _backupRepo, _opExecutor);
    }

    [Fact]
    public async Task BackupRepository_FullBackupAndRollbackPlanLifecycle()
    {
        var plan = new OperationPlan
        {
            Title = "Test Backup Session Plan",
            ProfileName = "Unit Test Profile",
            IsDryRun = false,
            Items = new List<OperationItem>
            {
                new()
                {
                    Name = "Sample Registry Tweak",
                    OperationType = OperationType.Registry,
                    Target = @"HKCU\Software\Win11ControlCenterTest",
                    OriginalValue = 0,
                    RecommendedValue = 1,
                    ValueType = "DWord",
                    Metadata = new Dictionary<string, string> { { "ValueName", "TestVal" } }
                }
            }
        };

        // 1. Create Backup
        var sessionPath = await _restoreService.CreateBackupAsync(plan);
        Assert.True(Directory.Exists(sessionPath));
        Assert.True(File.Exists(Path.Combine(sessionPath, "metadata.json")));
        Assert.True(File.Exists(Path.Combine(sessionPath, "operations.json")));
        Assert.True(File.Exists(Path.Combine(sessionPath, "registry", "pre_apply_rollback.reg")));

        var backupId = Path.GetFileName(sessionPath);

        // 2. Enumerate Backups
        var backups = await _restoreService.GetBackupsAsync();
        Assert.Contains(backups, b => b.BackupId == backupId);

        // 3. Rollback Backup
        bool restored = await _restoreService.RestoreBackupAsync(backupId);
        Assert.True(restored);

        // 4. Delete Backup
        bool deleted = await _restoreService.DeleteBackupAsync(backupId);
        Assert.True(deleted);
        Assert.False(Directory.Exists(sessionPath));
    }

    [Fact]
    public async Task LoggingService_AuditHistory_FullLifecycle()
    {
        // 1. Clear previous test entries
        await _logger.ClearAuditHistoryAsync();

        // 2. Log operations
        _logger.LogOperation(new OperationItem
        {
            Name = "Logged Tweak 1",
            Target = "Target 1",
            Status = ExecutionStatus.Succeeded,
            ResultMessage = "Success test"
        });

        _logger.LogOperation(new OperationItem
        {
            Name = "Logged Tweak 2",
            Target = "Target 2",
            Status = ExecutionStatus.Failed,
            ResultMessage = "Failed test"
        });

        // 3. Retrieve
        var history = await _logger.GetAuditHistoryAsync();
        Assert.Equal(2, history.Count);
        Assert.Contains(history, h => h.Name == "Logged Tweak 1" && h.Status == ExecutionStatus.Succeeded);
        Assert.Contains(history, h => h.Name == "Logged Tweak 2" && h.Status == ExecutionStatus.Failed);

        // 4. Export
        var tempFile = Path.Combine(Path.GetTempPath(), $"audit_test_{Guid.NewGuid():N}.json");
        try
        {
            var exportedPath = await _logger.ExportAuditHistoryAsync(tempFile);
            Assert.True(File.Exists(exportedPath));
            var content = await File.ReadAllTextAsync(exportedPath);
            Assert.Contains("Logged Tweak 1", content);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }

        // 5. Clear
        await _logger.ClearAuditHistoryAsync();
        var emptyHistory = await _logger.GetAuditHistoryAsync();
        Assert.Empty(emptyHistory);
    }
}
