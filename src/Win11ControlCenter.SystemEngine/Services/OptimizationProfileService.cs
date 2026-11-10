using System.Text.Json;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;

namespace Win11ControlCenter.SystemEngine.Services;

public class OptimizationProfileService : IOptimizationProfileService
{
    private readonly ILoggingService _logger;
    private readonly IPrivacyService _privacyService;
    private readonly ITweakEngineService _tweakService;
    private readonly IServiceManagerService _serviceManager;
    private readonly IOperationExecutor _operationExecutor;
    private readonly IPackageManagerService? _packageManager;
    private readonly string _profilesDirectory;

    public OptimizationProfileService(
        ILoggingService logger,
        IPrivacyService privacyService,
        ITweakEngineService tweakService,
        IServiceManagerService serviceManager,
        IOperationExecutor operationExecutor,
        IPackageManagerService? packageManager = null)
    {
        _logger = logger;
        _privacyService = privacyService;
        _tweakService = tweakService;
        _serviceManager = serviceManager;
        _operationExecutor = operationExecutor;
        _packageManager = packageManager;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _profilesDirectory = Path.Combine(appData, "Win11ControlCenter", "Profiles");
        Directory.CreateDirectory(_profilesDirectory);
    }

    public Task<IReadOnlyList<OptimizationProfile>> GetPredefinedProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = new List<OptimizationProfile>
        {
            new()
            {
                Id = "profile-safe-cleanup",
                Name = "Safe Cleanup",
                Icon = "🧹",
                Description = "Conservative baseline targeting advertising tracking and default explorer usability.",
                SafetyTier = "Tier 1: 100% Safe",
                ExpectedImpact = "Stops ad tracking IDs, enables file extensions, zero impact on system components.",
                RequiredPermissions = "Standard User",
                PrivacyTweakIds = new List<string>
                {
                    "disable-advertising-id",
                    "disable-feedback-frequency",
                    "disable-app-launch-tracking",
                    "disable-suggested-content-settings"
                },
                OptimizationTweakIds = new List<string>
                {
                    "show-file-extensions",
                    "show-hidden-files"
                },
                ServiceAdjustments = new Dictionary<string, string>
                {
                    { "RetailDemo", "Manual" }
                },
                TargetPowerPlanName = "Balanced"
            },
            new()
            {
                Id = "profile-balanced",
                Name = "Balanced Optimization",
                Icon = "⚖️",
                Description = "The recommended configuration for everyday work. Balances privacy, UI speed, and full Windows compatibility.",
                SafetyTier = "Tier 1: Safe & Reversible",
                ExpectedImpact = "Improves Explorer snappiness, limits telemetry to required baseline, disables promotional Start suggestions.",
                RequiredPermissions = "Standard User",
                PrivacyTweakIds = new List<string>
                {
                    "disable-advertising-id",
                    "disable-tailored-experiences",
                    "minimize-telemetry-data-collection",
                    "disable-activity-history-feed",
                    "disable-feedback-frequency",
                    "disable-start-bing-search",
                    "disable-app-launch-tracking",
                    "disable-suggested-content-settings",
                    "disable-start-suggestions"
                },
                OptimizationTweakIds = new List<string>
                {
                    "show-file-extensions",
                    "show-hidden-files",
                    "compact-view-explorer",
                    "speed-up-menu-show-delay",
                    "disable-windows-tips",
                    "disable-shake-minimize"
                },
                ServiceAdjustments = new Dictionary<string, string>
                {
                    { "RetailDemo", "Manual" },
                    { "MapsBroker", "Manual" }
                },
                TargetPowerPlanName = "Balanced"
            },
            new()
            {
                Id = "profile-gaming",
                Name = "Gaming Optimization",
                Icon = "🎮",
                Description = "Prioritizes gaming responsiveness: ensures Game Mode is active, eliminates background Game DVR stutter, and applies High Performance power.",
                SafetyTier = "Tier 1: Safe & Reversible",
                ExpectedImpact = "Eliminates background capture disk writes, reduces menu latency, and optimizes CPU responsiveness.",
                RequiredPermissions = "Standard User",
                PrivacyTweakIds = new List<string>
                {
                    "disable-advertising-id",
                    "disable-tailored-experiences",
                    "disable-feedback-frequency",
                    "disable-start-bing-search"
                },
                OptimizationTweakIds = new List<string>
                {
                    "enable-game-mode",
                    "disable-game-dvr-background-recording",
                    "disable-game-bar-app-capture",
                    "speed-up-menu-show-delay"
                },
                ServiceAdjustments = new Dictionary<string, string>
                {
                    { "RetailDemo", "Manual" },
                    { "MapsBroker", "Manual" }
                },
                TargetPowerPlanName = "High performance"
            },
            new()
            {
                Id = "profile-privacy-focused",
                Name = "Privacy Focused",
                Icon = "🔒",
                Description = "Strict privacy configuration: disables advertising IDs, telemetry down to minimum baseline, disables cloud sync, and suppresses web suggestions.",
                SafetyTier = "Tier 1: Safe & Reversible",
                ExpectedImpact = "Stops all ad personalization, restricts telemetry beacons, and keeps Start searches completely offline.",
                RequiredPermissions = "Standard User / Admin for HKLM Telemetry",
                PrivacyTweakIds = new List<string>
                {
                    "disable-advertising-id",
                    "disable-tailored-experiences",
                    "minimize-telemetry-data-collection",
                    "disable-activity-history-feed",
                    "disable-activity-history-publish",
                    "disable-feedback-frequency",
                    "disable-start-bing-search",
                    "disable-app-launch-tracking",
                    "disable-suggested-content-settings",
                    "disable-start-suggestions"
                },
                OptimizationTweakIds = new List<string>
                {
                    "disable-windows-tips"
                },
                ServiceAdjustments = new Dictionary<string, string>
                {
                    { "DiagTrack", "Manual" },
                    { "dmwappushservice", "Manual" },
                    { "RetailDemo", "Manual" }
                }
            },
            new()
            {
                Id = "profile-minimal",
                Name = "Minimal Windows",
                Icon = "⚡",
                Description = "Lean, distraction-free environment: full privacy baseline, full Explorer optimizations, and streamlined non-critical background services.",
                SafetyTier = "Tier 2: Moderate",
                ExpectedImpact = "Maximizes system responsiveness, lowest background activity without breaking Store or Windows Update.",
                RequiredPermissions = "Standard User / Admin",
                PrivacyTweakIds = new List<string>
                {
                    "disable-advertising-id",
                    "disable-tailored-experiences",
                    "minimize-telemetry-data-collection",
                    "disable-activity-history-feed",
                    "disable-activity-history-publish",
                    "disable-feedback-frequency",
                    "disable-start-bing-search",
                    "disable-app-launch-tracking",
                    "disable-suggested-content-settings",
                    "disable-start-suggestions"
                },
                OptimizationTweakIds = new List<string>
                {
                    "show-file-extensions",
                    "show-hidden-files",
                    "compact-view-explorer",
                    "explorer-launch-this-pc",
                    "show-seconds-taskbar-clock",
                    "speed-up-menu-show-delay",
                    "disable-windows-tips",
                    "disable-shake-minimize"
                },
                ServiceAdjustments = new Dictionary<string, string>
                {
                    { "DiagTrack", "Manual" },
                    { "dmwappushservice", "Manual" },
                    { "RetailDemo", "Manual" },
                    { "MapsBroker", "Manual" },
                    { "XblAuthManager", "Manual" },
                    { "XblGameSave", "Manual" },
                    { "XboxNetApiSvc", "Manual" }
                },
                TargetPowerPlanName = "High performance"
            }
        };

        return Task.FromResult((IReadOnlyList<OptimizationProfile>)profiles);
    }

    public async Task<IReadOnlyList<OptimizationProfile>> GetCustomProfilesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var customList = new List<OptimizationProfile>();
            if (!Directory.Exists(_profilesDirectory))
                return customList;

            var files = Directory.GetFiles(_profilesDirectory, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<OptimizationProfile>(json);
                    if (profile != null)
                    {
                        profile.IsCustom = true;
                        customList.Add(profile);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to deserialize custom profile '{file}': {ex.Message}", "Profiles");
                }
            }

            return (IReadOnlyList<OptimizationProfile>)customList;
        }, cancellationToken);
    }

    public async Task<ProfileDiffResult> CalculateProfileDiffAsync(OptimizationProfile profile, CancellationToken cancellationToken = default)
    {
        var diff = new ProfileDiffResult
        {
            ProfileId = profile.Id,
            ProfileName = profile.Name
        };

        // 1. Privacy Tweaks
        var privacyTweaks = await _privacyService.GetPrivacyTweaksAsync(cancellationToken);
        var privacyLookup = privacyTweaks.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var id in profile.PrivacyTweakIds)
        {
            if (privacyLookup.TryGetValue(id, out var pt))
            {
                string currStr = pt.CurrentValue?.ToString() ?? "(not configured)";
                string targetStr = pt.RecommendedValue?.ToString() ?? "(none)";
                bool willChange = currStr != targetStr;

                diff.Items.Add(new ProfileDiffItem
                {
                    Name = pt.Name,
                    Category = $"Privacy: {pt.Category}",
                    CurrentValue = currStr,
                    TargetValue = targetStr,
                    WillChange = willChange,
                    RiskLevel = pt.RiskLevel,
                    Scope = pt.ScopeDescription
                });
            }
        }

        // 2. Optimization / Explorer / Gaming Tweaks
        var explorerTweaks = await _tweakService.GetExplorerTweaksAsync(cancellationToken);
        var gamingTweaks = await _tweakService.GetGamingTweaksAsync(cancellationToken);
        var optLookup = explorerTweaks.Concat(gamingTweaks).ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var id in profile.OptimizationTweakIds)
        {
            if (optLookup.TryGetValue(id, out var ot))
            {
                string currStr = ot.CurrentValue?.ToString() ?? "(not configured)";
                string targetStr = ot.RecommendedValue?.ToString() ?? "(none)";
                bool willChange = currStr != targetStr;

                diff.Items.Add(new ProfileDiffItem
                {
                    Name = ot.Name,
                    Category = $"Optimization: {ot.Category}",
                    CurrentValue = currStr,
                    TargetValue = targetStr,
                    WillChange = willChange,
                    RiskLevel = ot.RiskLevel,
                    Scope = ot.ScopeDescription
                });
            }
        }

        // 3. Service Adjustments
        var allServices = await _serviceManager.GetServicesAsync(cancellationToken);
        var svcLookup = allServices.ToDictionary(s => s.ServiceName, StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in profile.ServiceAdjustments)
        {
            string svcName = kvp.Key;
            string targetStartup = kvp.Value;

            if (svcLookup.TryGetValue(svcName, out var svc))
            {
                bool willChange = !svc.StartupType.Equals(targetStartup, StringComparison.OrdinalIgnoreCase);

                diff.Items.Add(new ProfileDiffItem
                {
                    Name = svc.DisplayName,
                    Category = "Windows Service",
                    CurrentValue = svc.StartupType,
                    TargetValue = targetStartup,
                    WillChange = willChange,
                    RiskLevel = CoreWindowsServices.IsProtectedService(svcName) ? OperationRiskLevel.Critical : OperationRiskLevel.Safe,
                    Scope = "Machine-wide"
                });
            }
        }

        // 4. Power Plan
        if (!string.IsNullOrWhiteSpace(profile.TargetPowerPlanName))
        {
            var plans = await _tweakService.GetPowerPlansAsync(cancellationToken);
            var activePlan = plans.FirstOrDefault(p => p.IsActive);
            var targetPlan = plans.FirstOrDefault(p => p.Name.Contains(profile.TargetPowerPlanName, StringComparison.OrdinalIgnoreCase));

            if (targetPlan != null)
            {
                bool willChange = activePlan?.Guid != targetPlan.Guid;
                diff.Items.Add(new ProfileDiffItem
                {
                    Name = "Active Power Scheme",
                    Category = "Power",
                    CurrentValue = activePlan?.Name ?? "Unknown",
                    TargetValue = targetPlan.Name,
                    WillChange = willChange,
                    RiskLevel = OperationRiskLevel.Safe,
                    Scope = "Current User"
                });
            }
        }

        return diff;
    }

    public async Task<OperationPlan> CreateProfilePlanAsync(OptimizationProfile profile, bool isDryRun, CancellationToken cancellationToken = default)
    {
        var requestedItems = new List<OperationItem>();

        // 1. Privacy Items
        var privacyTweaks = await _privacyService.GetPrivacyTweaksAsync(cancellationToken);
        var privacyLookup = privacyTweaks.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var id in profile.PrivacyTweakIds)
        {
            if (privacyLookup.TryGetValue(id, out var pt))
            {
                requestedItems.Add(new OperationItem
                {
                    Name = pt.Name,
                    Description = pt.Description,
                    Category = $"Privacy: {pt.Category}",
                    RiskLevel = pt.RiskLevel,
                    OperationType = OperationType.Registry,
                    Reversibility = ReversibilityTier.FullyReversible,
                    Target = $"{pt.RegistryHive}\\{pt.KeyPath}",
                    OriginalValue = pt.CurrentValue,
                    RecommendedValue = pt.RecommendedValue,
                    ValueType = pt.ValueType,
                    RequiresAdmin = pt.RequiresAdmin,
                    RequiresRestart = pt.RequiresRestart,
                    RollbackSupported = true,
                    Metadata = new Dictionary<string, string>
                    {
                        { "TweakId", pt.Id },
                        { "ValueName", pt.ValueName }
                    }
                });
            }
        }

        // 2. Optimization Items
        var explorerTweaks = await _tweakService.GetExplorerTweaksAsync(cancellationToken);
        var gamingTweaks = await _tweakService.GetGamingTweaksAsync(cancellationToken);
        var optLookup = explorerTweaks.Concat(gamingTweaks).ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var id in profile.OptimizationTweakIds)
        {
            if (optLookup.TryGetValue(id, out var ot))
            {
                requestedItems.Add(new OperationItem
                {
                    Name = ot.Name,
                    Description = ot.Description,
                    Category = $"Optimization: {ot.Category}",
                    RiskLevel = ot.RiskLevel,
                    OperationType = OperationType.Registry,
                    Reversibility = ReversibilityTier.FullyReversible,
                    Target = $"{ot.RegistryHive}\\{ot.KeyPath}",
                    OriginalValue = ot.CurrentValue,
                    RecommendedValue = ot.RecommendedValue,
                    ValueType = ot.ValueType,
                    RequiresAdmin = ot.RequiresAdmin,
                    RequiresRestart = ot.RequiresRestart,
                    RollbackSupported = true,
                    Metadata = new Dictionary<string, string>
                    {
                        { "TweakId", ot.Id },
                        { "ValueName", ot.ValueName }
                    }
                });
            }
        }

        // 3. Service Adjustments
        var allServices = await _serviceManager.GetServicesAsync(cancellationToken);
        var svcLookup = allServices.ToDictionary(s => s.ServiceName, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in profile.ServiceAdjustments)
        {
            if (svcLookup.TryGetValue(kvp.Key, out var svc))
            {
                requestedItems.Add(new OperationItem
                {
                    Name = $"Service: {svc.DisplayName}",
                    Description = $"Configure {svc.DisplayName} startup to {kvp.Value}.",
                    Category = "Windows Service",
                    RiskLevel = CoreWindowsServices.IsProtectedService(svc.ServiceName) ? OperationRiskLevel.Critical : OperationRiskLevel.Safe,
                    OperationType = OperationType.Service,
                    Reversibility = ReversibilityTier.FullyReversible,
                    Target = svc.ServiceName,
                    OriginalValue = svc.StartupType,
                    RecommendedValue = kvp.Value,
                    RequiresAdmin = true,
                    RollbackSupported = true,
                    Metadata = new Dictionary<string, string>
                    {
                        { "Action", "ConfigureStartup" },
                        { "DesiredStartupType", kvp.Value }
                    }
                });
            }
        }

        return await _operationExecutor.GeneratePlanAsync(requestedItems, profile.Name, isDryRun);
    }

    public async Task<bool> SaveCustomProfileAsync(OptimizationProfile profile, CancellationToken cancellationToken = default)
    {
        try
        {
            profile.IsCustom = true;
            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                profile.Id = "custom-" + Guid.NewGuid().ToString("N")[..8];
            }

            var filePath = Path.Combine(_profilesDirectory, $"{profile.Id}.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            _logger.LogInfo($"Saved custom optimization profile '{profile.Name}' ({profile.Id})", "Profiles");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save profile '{profile.Name}'", ex, "Profiles");
            return false;
        }
    }

    public Task<bool> DeleteCustomProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = Path.Combine(_profilesDirectory, $"{profileId}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInfo($"Deleted custom profile: {profileId}", "Profiles");
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to delete custom profile {profileId}", ex, "Profiles");
            return Task.FromResult(false);
        }
    }

    public Task<OptimizationProfile?> ImportProfileFromJsonAsync(string jsonContent)
    {
        try
        {
            var profile = JsonSerializer.Deserialize<OptimizationProfile>(jsonContent);
            return Task.FromResult(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to import profile from JSON", ex, "Profiles");
            return Task.FromResult<OptimizationProfile?>(null);
        }
    }

    public Task<string> ExportProfileToJsonAsync(OptimizationProfile profile)
    {
        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(json);
    }

    public async Task<string> ExportPortableProfileToFileAsync(PortableMachineProfile profile, string destinationFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(profile, options);
            var dir = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(destinationFilePath, json, cancellationToken);
            _logger.LogInfo($"Exported portable machine profile '{profile.Name}' to '{destinationFilePath}'.", "Profiles");
            return destinationFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to export portable profile to '{destinationFilePath}'", ex, "Profiles");
            throw;
        }
    }

    public async Task<PortableMachineProfile?> ImportPortableProfileFromFileAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFilePath))
        {
            _logger.LogWarning($"Portable profile file not found: '{sourceFilePath}'", "Profiles");
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(sourceFilePath, cancellationToken);
            var profile = JsonSerializer.Deserialize<PortableMachineProfile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (profile == null) return null;

            // --- CRITICAL SAFETY SHIELD SANITIZATION ---
            // 1. Sanitize Service Adjustments: Never allow tampering with core Windows services
            var blockedServices = profile.ServiceAdjustments.Keys
                .Where(CoreWindowsServices.IsProtectedService)
                .ToList();

            foreach (var blockedSvc in blockedServices)
            {
                profile.ServiceAdjustments.Remove(blockedSvc);
                _logger.LogWarning($"Safety Shield stripped protected service '{blockedSvc}' from imported profile '{profile.Name}'.", "SafetyValidator");
            }

            // 2. Sanitize Package Removals: Never allow removal of protected infrastructure packages
            var blockedPackages = profile.DebloatPackageNames
                .Where(ProtectedPackages.IsProtected)
                .ToList();

            foreach (var blockedPkg in blockedPackages)
            {
                profile.DebloatPackageNames.Remove(blockedPkg);
                _logger.LogWarning($"Safety Shield stripped protected package '{blockedPkg}' from imported profile '{profile.Name}'.", "SafetyValidator");
            }

            // 3. Fallback name if missing
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                profile.Name = Path.GetFileNameWithoutExtension(sourceFilePath);
            }

            _logger.LogInfo($"Successfully imported and sanitized portable profile '{profile.Name}' ({profile.TotalActionCount} actions).", "Profiles");
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to deserialize portable profile from '{sourceFilePath}'", ex, "Profiles");
            return null;
        }
    }

    public async Task<PortableMachineProfile> CaptureCurrentSystemProfileAsync(string name, string description, CancellationToken cancellationToken = default)
    {
        var profile = new PortableMachineProfile
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"System Snapshot ({DateTime.Now:yyyy-MM-dd})" : name,
            Description = string.IsNullOrWhiteSpace(description) ? "Captured active configuration of current Windows 11 installation." : description,
            Author = Environment.UserName,
            ExportedAtUtc = DateTime.UtcNow,
            Icon = "🖥️",
            SafetyTier = "Tier 1: Safe & Reversible"
        };

        try
        {
            // 1. Capture applied privacy tweaks
            var privacyTweaks = await _privacyService.GetPrivacyTweaksAsync(cancellationToken);
            profile.PrivacyTweakIds = privacyTweaks
                .Where(t => t.IsApplied)
                .Select(t => t.Id)
                .ToList();

            // 2. Capture applied explorer & gaming tweaks
            var explorerTweaks = await _tweakService.GetExplorerTweaksAsync(cancellationToken);
            var gamingTweaks = await _tweakService.GetGamingTweaksAsync(cancellationToken);
            profile.OptimizationTweakIds = explorerTweaks.Concat(gamingTweaks)
                .Where(t => t.IsApplied)
                .Select(t => t.Id)
                .ToList();

            // 3. Capture active power plan
            var powerPlans = await _tweakService.GetPowerPlansAsync(cancellationToken);
            var activePlan = powerPlans.FirstOrDefault(p => p.IsActive);
            if (activePlan != null)
            {
                profile.TargetPowerPlanName = activePlan.Name;
            }

            // 4. Capture non-default / adjusted Windows services
            try
            {
                var services = await _serviceManager.GetServicesAsync(cancellationToken);
                foreach (var s in services.Where(s => !s.IsCoreService && !CoreWindowsServices.IsProtectedService(s.ServiceName)))
                {
                    if (s.StartupType != s.OriginalStartupType && !string.IsNullOrWhiteSpace(s.StartupType))
                    {
                        profile.ServiceAdjustments[s.ServiceName] = s.StartupType;
                    }
                }
            }
            catch { }

            _logger.LogInfo($"Captured current system profile with {profile.TotalActionCount} active settings.", "Profiles");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while capturing current system profile", ex, "Profiles");
        }

        return profile;
    }

    public async Task<OperationPlan> CreatePortableProfileExecutionPlanAsync(PortableMachineProfile profile, bool isDryRun, CancellationToken cancellationToken = default)
    {
        var optProfile = profile.ToOptimizationProfile();
        var plan = await CreateProfilePlanAsync(optProfile, isDryRun, cancellationToken);

        // If profile specifies debloat packages, add them
        if (_packageManager != null && profile.DebloatPackageNames.Count > 0)
        {
            try
            {
                var installed = await _packageManager.GetInstalledPackagesAsync(cancellationToken);
                var targetPackages = installed
                    .Where(p => profile.DebloatPackageNames.Any(name =>
                        string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.PackageFullName, name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.DisplayName, name, StringComparison.OrdinalIgnoreCase)))
                    .Where(p => !p.IsProtected)
                    .ToList();

                if (targetPackages.Count > 0)
                {
                    foreach (var pkg in targetPackages)
                    {
                        pkg.IsSelectedForRemoval = true;
                    }
                    var removalPlan = await _packageManager.CreateRemovalPlanAsync(targetPackages, isDryRun);
                    foreach (var item in removalPlan.Items)
                    {
                        plan.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not resolve packages for profile execution: {ex.Message}", "Profiles");
            }
        }

        if (profile.DisabledStartupItems.Count > 0)
        {
            foreach (var startupName in profile.DisabledStartupItems)
            {
                plan.Items.Add(new OperationItem
                {
                    Name = $"Disable startup: {startupName}",
                    Description = $"Ensure startup entry '{startupName}' is disabled.",
                    Category = "Startup Application",
                    RiskLevel = OperationRiskLevel.Safe,
                    OperationType = OperationType.Startup,
                    Target = startupName,
                    RecommendedValue = false,
                    RollbackSupported = true,
                    Metadata = new Dictionary<string, string>
                    {
                        { "LocationType", "RegistryCurrentUser" }
                    }
                });
            }
        }

        return plan;
    }
}

