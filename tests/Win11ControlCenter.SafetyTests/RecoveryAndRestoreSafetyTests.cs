using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.SafetyTests;

public class RecoveryAndRestoreSafetyTests
{
    [Theory]
    [InlineData("MyRestorePoint; Remove-Item C:\\ -Recurse")]
    [InlineData("Checkpoint | Invoke-Expression 'malicious'")]
    [InlineData("Normal Checkpoint 123")]
    public async Task RestoreService_SanitizesRestorePointDescription(string rawDescription)
    {
        var logger = new LoggingService();
        var backupRepo = new BackupRepository();
        var opExec = new OperationExecutor(logger, backupRepo);
        var restoreService = new RestoreService(logger, backupRepo, opExec);

        // This will attempt creation or safely fail if not elevated, but MUST NOT throw an unhandled syntax or injection exception
        var exception = await Record.ExceptionAsync(() => restoreService.CreateSystemRestorePointAsync(rawDescription));

        Assert.Null(exception);
    }
}

