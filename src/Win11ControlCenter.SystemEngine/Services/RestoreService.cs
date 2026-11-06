using System.Diagnostics;
using System.Text.RegularExpressions;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.PowerShell.Runner;
using Win11ControlCenter.SystemEngine.Handlers;

namespace Win11ControlCenter.SystemEngine.Services;

public class RestoreService : IRestoreService
{
    private static readonly Regex SafeDescriptionRegex = new(@"^[a-zA-Z0-9\s\-_]+$", RegexOptions.Compiled);

    private readonly ILoggingService _logger;
    private readonly BackupRepository _backupRepository;
    private readonly IOperationExecutor _operationExecutor;

    public RestoreService(
        ILoggingService logger,
        BackupRepository backupRepository,
        IOperationExecutor operationExecutor)
    {
        _logger = logger;
        _backupRepository = backupRepository;
        _operationExecutor = operationExecutor;
    }

    public Task<IReadOnlyList<BackupMetadata>> GetBackupsAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => _backupRepository.GetAllBackups(), cancellationToken);
    }

    public Task<string> CreateBackupAsync(OperationPlan plan, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => _backupRepository.CreateBackupSession(plan), cancellationToken);
    }

    public async Task<bool> RestoreBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Initiating restore for backup session '{backupId}'", "RestoreService");

        var backupPath = Path.Combine(_backupRepository.GetBackupsRoot(), backupId);
        if (!Directory.Exists(backupPath))
        {
            _logger.LogError($"Backup directory '{backupPath}' not found.", null, "RestoreService");
            return false;
        }

        var plan = _backupRepository.LoadBackupPlan(backupPath);
        if (plan == null)
        {
            _logger.LogError($"Failed to load operation plan from backup '{backupId}'.", null, "RestoreService");
            return false;
        }

        try
        {
            // 1. Rollback via typed operation executor
            bool success = await _operationExecutor.RollbackPlanAsync(plan, null, cancellationToken);

            // 2. Also import emergency .reg file if available for complete registry fidelity
            var regFile = Path.Combine(backupPath, "registry", "pre_apply_rollback.reg");
            if (File.Exists(regFile))
            {
                ApplyRegistryFile(regFile);
            }

            if (success)
            {
                _logger.LogInfo($"Successfully restored backup session: {backupId}", "RestoreService");
            }
            else
            {
                _logger.LogWarning($"Backup session {backupId} restored with one or more warnings.", "RestoreService");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed during restoration of backup '{backupId}'", ex, "RestoreService");
            return false;
        }
    }

    public Task<bool> DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            bool deleted = _backupRepository.DeleteBackupSession(backupId);
            if (deleted)
            {
                _logger.LogInfo($"Deleted backup session: {backupId}", "RestoreService");
            }
            return deleted;
        }, cancellationToken);
    }

    public async Task<bool> CreateSystemRestorePointAsync(string description, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description) || !SafeDescriptionRegex.IsMatch(description))
        {
            description = "Win11ControlCenter Checkpoint";
        }

        _logger.LogInfo($"Requesting System Restore checkpoint: '{description}'", "SystemRestore");

        try
        {
            var command = $"Checkpoint-Computer -Description \"{description}\" -RestorePointType \"MODIFY_SETTINGS\"";
            var (success, output, error) = await PowerShellCommandRunner.RunSafeCommandAsync(command, cancellationToken);

            if (success)
            {
                _logger.LogInfo($"System Restore checkpoint created: '{description}'", "SystemRestore");
                return true;
            }

            _logger.LogWarning($"System Restore checkpoint could not be created: {error}. (Note: Creating System Restore points requires Administrator elevation and System Protection enabled).", "SystemRestore");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception triggering System Restore checkpoint", ex, "SystemRestore");
            return false;
        }
    }

    public bool IsSystemRestoreAvailable()
    {
        try
        {
            var disableSr = RegistryOperationHandler.ReadCurrentValue(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", "DisableSR");
            if (disableSr is int d && d == 1)
            {
                return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyRegistryFile(string regFilePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = $"import \"{regFilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(3000);
        }
        catch
        {
            // Ignore secondary reg.exe failure
        }
    }
}

