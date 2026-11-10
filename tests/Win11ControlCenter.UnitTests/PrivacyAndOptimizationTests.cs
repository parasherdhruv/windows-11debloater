using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class PrivacyAndOptimizationTests
{
    private readonly LoggingService _logger;
    private readonly BackupRepository _backupRepo;
    private readonly OperationExecutor _opExecutor;
    private readonly PrivacyService _privacyService;
    private readonly TweakEngineService _tweakService;
    private readonly ServiceManagerService _serviceManager;
    private readonly OptimizationProfileService _profileService;

    public PrivacyAndOptimizationTests()
    {
        _logger = new LoggingService();
        _backupRepo = new BackupRepository();
        _opExecutor = new OperationExecutor(_logger, _backupRepo);
        _privacyService = new PrivacyService(_logger, _opExecutor);
        _tweakService = new TweakEngineService(_logger, _opExecutor);
        _serviceManager = new ServiceManagerService(_logger);
        _profileService = new OptimizationProfileService(_logger, _privacyService, _tweakService, _serviceManager, _opExecutor);
    }

    [Fact]
    public async Task PrivacyService_ReturnsDocumentedCatalog()
    {
        var tweaks = await _privacyService.GetPrivacyTweaksAsync();

        Assert.NotEmpty(tweaks);
        Assert.True(tweaks.Count >= 9);
        Assert.Contains(tweaks, t => t.Id == "disable-advertising-id");
        Assert.Contains(tweaks, t => t.Id == "disable-tailored-experiences");
        Assert.Contains(tweaks, t => t.Id == "minimize-telemetry-data-collection");
    }

    [Fact]
    public async Task PrivacyService_CreatePrivacyPlan_GeneratesValidPlan()
    {
        var tweaks = await _privacyService.GetPrivacyTweaksAsync();
        var plan = await _privacyService.CreatePrivacyPlanAsync(tweaks.Take(3), isDryRun: true);

        Assert.NotNull(plan);
        Assert.Equal(3, plan.Items.Count);
        Assert.True(plan.IsDryRun);
        Assert.All(plan.Items, item =>
        {
            Assert.Equal(OperationType.Registry, item.OperationType);
            Assert.True(item.RollbackSupported);
            Assert.Equal(ReversibilityTier.FullyReversible, item.Reversibility);
        });
    }

    [Fact]
    public async Task PrivacyService_CreateRevertPlan_ConfiguresDefaults()
    {
        var tweaks = await _privacyService.GetPrivacyTweaksAsync();
        var plan = await _privacyService.CreateRevertPlanAsync(tweaks.Take(2), isDryRun: false);

        Assert.NotNull(plan);
        Assert.Equal(2, plan.Items.Count);
        Assert.StartsWith("Revert:", plan.Items[0].Name);
        Assert.NotNull(plan.Items[0].RecommendedValue);
    }

    [Fact]
    public async Task TweakEngine_ReturnsExplorerTweaks()
    {
        var tweaks = await _tweakService.GetExplorerTweaksAsync();

        Assert.NotEmpty(tweaks);
        Assert.Contains(tweaks, t => t.Id == "show-file-extensions");
        Assert.Contains(tweaks, t => t.Id == "show-hidden-files");
        Assert.Contains(tweaks, t => t.Id == "compact-view-explorer");
        Assert.Contains(tweaks, t => t.Id == "disable-bing-start-search");
        Assert.Contains(tweaks, t => t.Id == "enable-taskbar-end-task");
        Assert.Contains(tweaks, t => t.Id == "hide-recommended-start");
    }

    [Fact]
    public async Task TweakEngine_EnumeratesPowerPlansSafely()
    {
        var plans = await _tweakService.GetPowerPlansAsync();

        Assert.NotNull(plans);
        // On Windows 11 there's always at least 1 power plan
        Assert.NotEmpty(plans);
        Assert.Contains(plans, p => p.IsActive);
        Assert.All(plans, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Guid));
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
        });
    }

    [Fact]
    public async Task ProfileService_Returns5PredefinedProfiles()
    {
        var profiles = await _profileService.GetPredefinedProfilesAsync();

        Assert.NotNull(profiles);
        Assert.Equal(5, profiles.Count);

        var names = profiles.Select(p => p.Name).ToList();
        Assert.Contains("Safe Cleanup", names);
        Assert.Contains("Balanced Optimization", names);
        Assert.Contains("Gaming Optimization", names);
        Assert.Contains("Privacy Focused", names);
        Assert.Contains("Minimal Windows", names);
    }

    [Fact]
    public async Task ProfileService_CalculateProfileDiff_ReturnsAccurateDiff()
    {
        var profiles = await _profileService.GetPredefinedProfilesAsync();
        var balanced = profiles.First(p => p.Id == "profile-balanced");

        var diff = await _profileService.CalculateProfileDiffAsync(balanced);

        Assert.NotNull(diff);
        Assert.Equal(balanced.Name, diff.ProfileName);
        Assert.NotEmpty(diff.Items);
        Assert.True(diff.TotalItems > 0);
        Assert.Equal(diff.TotalItems, diff.ItemsToChange + diff.ItemsAlreadyCompliant);
    }

    [Fact]
    public async Task ProfileService_CreateProfilePlan_CreatesValidExecutionPlan()
    {
        var profiles = await _profileService.GetPredefinedProfilesAsync();
        var safeProfile = profiles.First(p => p.Id == "profile-safe-cleanup");

        var plan = await _profileService.CreateProfilePlanAsync(safeProfile, isDryRun: true);

        Assert.NotNull(plan);
        Assert.True(plan.IsDryRun);
        Assert.NotEmpty(plan.Items);
        Assert.All(plan.Items, item =>
        {
            Assert.NotEqual(OperationType.FileCleanup, item.OperationType);
            Assert.True(item.RollbackSupported);
        });
    }

    [Fact]
    public async Task ProfileService_CustomProfile_SaveLoadDeleteLifecycle()
    {
        var testProfile = new OptimizationProfile
        {
            Id = "custom-test-unit",
            Name = "Unit Test Profile",
            Description = "A temporary test profile",
            PrivacyTweakIds = new List<string> { "disable-advertising-id" },
            OptimizationTweakIds = new List<string> { "show-file-extensions" }
        };

        // 1. Save
        bool saved = await _profileService.SaveCustomProfileAsync(testProfile);
        Assert.True(saved);

        // 2. Load
        var customs = await _profileService.GetCustomProfilesAsync();
        Assert.Contains(customs, c => c.Id == testProfile.Id);

        // 3. Export & Import JSON
        string json = await _profileService.ExportProfileToJsonAsync(testProfile);
        Assert.Contains("Unit Test Profile", json);
        var imported = await _profileService.ImportProfileFromJsonAsync(json);
        Assert.NotNull(imported);
        Assert.Equal(testProfile.Name, imported.Name);

        // 4. Delete
        bool deleted = await _profileService.DeleteCustomProfileAsync(testProfile.Id);
        Assert.True(deleted);

        // Verify gone
        var remaining = await _profileService.GetCustomProfilesAsync();
        Assert.DoesNotContain(remaining, c => c.Id == testProfile.Id);
    }
}

