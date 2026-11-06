using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.SafetyTests;

public class RegistryAndTweakSafetyTests
{
    [Theory]
    [InlineData(@"HKLM\SAM")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SAM\SAM")]
    [InlineData(@"HKLM\SECURITY")]
    [InlineData(@"HKLM\BCD00000000")]
    [InlineData(@"HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal")]
    [InlineData(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\KnownDLLs")]
    public void SafetyValidator_MustBlockDangerousRegistryTargets(string dangerousPath)
    {
        var item = new OperationItem
        {
            Name = "Dangerous Tweak",
            OperationType = OperationType.Registry,
            Target = dangerousPath,
            RecommendedValue = 0
        };

        var (isValid, reason) = SafetyValidator.ValidateOperation(item);

        Assert.False(isValid, $"SafetyValidator should have blocked dangerous target: {dangerousPath}");
        Assert.Contains("protected", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OptimizationProfiles_MustNeverTouchCoreProtectedServices()
    {
        var logger = new LoggingService();
        var backupRepo = new BackupRepository();
        var opExec = new OperationExecutor(logger, backupRepo);
        var privService = new PrivacyService(logger, opExec);
        var tweakService = new TweakEngineService(logger, opExec);
        var svcManager = new ServiceManagerService(logger);

        var profileService = new OptimizationProfileService(logger, privService, tweakService, svcManager, opExec);

        var profiles = await profileService.GetPredefinedProfilesAsync();

        foreach (var profile in profiles)
        {
            foreach (var svcName in profile.ServiceAdjustments.Keys)
            {
                Assert.False(CoreWindowsServices.IsProtectedService(svcName),
                    $"Profile '{profile.Name}' attempts to modify protected core Windows service '{svcName}'!");
            }
        }
    }

    [Fact]
    public void PrivacyCatalog_AllTweaksMustBeSafeRiskLevel()
    {
        var catalog = PrivacyService.GetSupportedPrivacyCatalog();

        Assert.NotEmpty(catalog);
        foreach (var tweak in catalog)
        {
            Assert.Equal(OperationRiskLevel.Safe, tweak.RiskLevel);
            Assert.False(string.IsNullOrWhiteSpace(tweak.Id));
            Assert.False(string.IsNullOrWhiteSpace(tweak.KeyPath));
            Assert.False(string.IsNullOrWhiteSpace(tweak.ValueName));
            Assert.NotNull(tweak.RecommendedValue);
            Assert.NotNull(tweak.DefaultValue);
        }
    }

    [Fact]
    public void ExplorerCatalog_AllTweaksMustBeSafeRiskLevel()
    {
        var catalog = TweakEngineService.GetSupportedExplorerCatalog();

        Assert.NotEmpty(catalog);
        foreach (var tweak in catalog)
        {
            Assert.Equal(OperationRiskLevel.Safe, tweak.RiskLevel);
            Assert.False(string.IsNullOrWhiteSpace(tweak.Id));
            Assert.False(string.IsNullOrWhiteSpace(tweak.KeyPath));
            Assert.NotNull(tweak.RecommendedValue);
            Assert.NotNull(tweak.DefaultValue);
        }
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("12345")]
    [InlineData("381b4222-f694-41f0-9685-ff5bb260df2e; rm -rf")]
    [InlineData("../../../evil")]
    public async Task TweakEngine_RejectsInvalidPowerPlanGuid(string invalidGuid)
    {
        var logger = new LoggingService();
        var backupRepo = new BackupRepository();
        var opExec = new OperationExecutor(logger, backupRepo);
        var tweakService = new TweakEngineService(logger, opExec);

        bool result = await tweakService.SetActivePowerPlanAsync(invalidGuid);

        Assert.False(result, $"Power plan switch should reject invalid GUID: {invalidGuid}");
    }
}

