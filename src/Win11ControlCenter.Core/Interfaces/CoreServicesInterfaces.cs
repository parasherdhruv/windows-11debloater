using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.Core.Interfaces;

public interface IPackageManagerService
{
    Task<IReadOnlyList<ApplicationPackage>> GetInstalledPackagesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationPackage>> GetProvisionedPackagesAsync(CancellationToken cancellationToken = default);
    Task<OperationPlan> CreateRemovalPlanAsync(IEnumerable<ApplicationPackage> packages, bool isDryRun);
}

public interface IServiceManagerService
{
    Task<IReadOnlyList<WindowsServiceItem>> GetServicesAsync(CancellationToken cancellationToken = default);
    Task<bool> SetStartupTypeAsync(string serviceName, string startupType, CancellationToken cancellationToken = default);
    Task<bool> StartServiceAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<bool> StopServiceAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<string> ExportServicesInventoryAsync(IEnumerable<WindowsServiceItem> services, string destinationPath, CancellationToken cancellationToken = default);
}

public interface IStartupManagerService
{
    Task<IReadOnlyList<StartupEntry>> GetStartupEntriesAsync(CancellationToken cancellationToken = default);
    Task<bool> SetStartupEntryStateAsync(StartupEntry entry, bool enable, CancellationToken cancellationToken = default);
    void OpenFileLocation(StartupEntry entry);
    Task<string> ExportStartupInventoryAsync(IEnumerable<StartupEntry> entries, string destinationPath, CancellationToken cancellationToken = default);
}

public interface IPrivacyService
{
    Task<IReadOnlyList<PrivacyTweak>> GetPrivacyTweaksAsync(CancellationToken cancellationToken = default);
    Task<OperationPlan> CreatePrivacyPlanAsync(IEnumerable<PrivacyTweak> tweaks, bool isDryRun);
    Task<OperationPlan> CreateRevertPlanAsync(IEnumerable<PrivacyTweak> tweaks, bool isDryRun);
}

public interface ITweakEngineService
{
    Task<IReadOnlyList<OptimizationTweak>> GetExplorerTweaksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OptimizationTweak>> GetGamingTweaksAsync(CancellationToken cancellationToken = default);
    Task<OperationPlan> CreateOptimizationPlanAsync(IEnumerable<OptimizationTweak> tweaks, bool isDryRun);
    Task<IReadOnlyList<PowerPlanInfo>> GetPowerPlansAsync(CancellationToken cancellationToken = default);
    Task<bool> SetActivePowerPlanAsync(string planGuid, CancellationToken cancellationToken = default);
}

public interface IOptimizationProfileService
{
    Task<IReadOnlyList<OptimizationProfile>> GetPredefinedProfilesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OptimizationProfile>> GetCustomProfilesAsync(CancellationToken cancellationToken = default);
    Task<ProfileDiffResult> CalculateProfileDiffAsync(OptimizationProfile profile, CancellationToken cancellationToken = default);
    Task<OperationPlan> CreateProfilePlanAsync(OptimizationProfile profile, bool isDryRun, CancellationToken cancellationToken = default);
    Task<bool> SaveCustomProfileAsync(OptimizationProfile profile, CancellationToken cancellationToken = default);
    Task<bool> DeleteCustomProfileAsync(string profileId, CancellationToken cancellationToken = default);
    Task<OptimizationProfile?> ImportProfileFromJsonAsync(string jsonContent);
    Task<string> ExportProfileToJsonAsync(OptimizationProfile profile);
}

public interface ICleanupService
{
    Task<IReadOnlyList<CleanupTarget>> ScanCleanupTargetsAsync(CancellationToken cancellationToken = default);
    Task<OperationPlan> CreateCleanupPlanAsync(IEnumerable<CleanupTarget> targets, bool isDryRun);
}

public interface IRestoreService
{
    Task<IReadOnlyList<BackupMetadata>> GetBackupsAsync(CancellationToken cancellationToken = default);
    Task<string> CreateBackupAsync(OperationPlan plan, CancellationToken cancellationToken = default);
    Task<bool> RestoreBackupAsync(string backupId, CancellationToken cancellationToken = default);
    Task<bool> DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default);
    Task<bool> CreateSystemRestorePointAsync(string description, CancellationToken cancellationToken = default);
    bool IsSystemRestoreAvailable();
}

public interface ILoggingService
{
    void LogInfo(string message, string? category = null);
    void LogWarning(string message, string? category = null);
    void LogError(string message, Exception? ex = null, string? category = null);
    void LogOperation(OperationItem item);
    Task<IReadOnlyList<OperationItem>> GetAuditHistoryAsync(CancellationToken cancellationToken = default);
    Task<bool> ClearAuditHistoryAsync(CancellationToken cancellationToken = default);
    Task<string> ExportAuditHistoryAsync(string destinationPath, CancellationToken cancellationToken = default);
}

