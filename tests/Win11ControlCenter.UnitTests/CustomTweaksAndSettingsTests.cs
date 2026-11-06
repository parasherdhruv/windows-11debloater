using Win11ControlCenter.App.ViewModels;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class CustomTweaksAndSettingsTests
{
    private readonly LoggingService _logger;
    private readonly BackupRepository _backupRepo;
    private readonly OperationExecutor _opExecutor;
    private readonly SettingsStorage _settingsStorage;

    public CustomTweaksAndSettingsTests()
    {
        _logger = new LoggingService();
        _backupRepo = new BackupRepository();
        _opExecutor = new OperationExecutor(_logger, _backupRepo);
        _settingsStorage = new SettingsStorage();
    }

    [Fact]
    public async Task CustomTweaksViewModel_AddAndRemoveLifecycle()
    {
        var vm = new CustomTweaksViewModel(_logger, _opExecutor);
        await vm.InitializeAsync();

        // 1. Invalid input: empty name
        vm.NewName = "";
        vm.NewKeyPath = @"Software\TestTweak";
        vm.NewValueName = "Setting";
        await vm.AddTweakCommand.ExecuteAsync(null);
        Assert.False(string.IsNullOrWhiteSpace(vm.ValidationError));

        // 2. Dangerous input: SAM hive
        vm.NewName = "Malicious Tweak";
        vm.NewHive = "HKLM";
        vm.NewKeyPath = @"SAM\Config";
        vm.NewValueName = "Val";
        await vm.AddTweakCommand.ExecuteAsync(null);
        Assert.Contains("Safety Block", vm.ValidationError);

        // 3. Valid input
        vm.NewName = "Unit Test Valid Tweak";
        vm.NewHive = "HKCU";
        vm.NewKeyPath = @"Software\Win11ControlCenterUnitTests";
        vm.NewValueName = "Active";
        vm.NewValueType = "DWord";
        vm.NewRecommendedValue = "1";
        vm.NewDefaultValue = "0";

        await vm.AddTweakCommand.ExecuteAsync(null);
        Assert.Empty(vm.ValidationError);

        var added = vm.CustomTweaks.FirstOrDefault(t => t.Name == "Unit Test Valid Tweak");
        Assert.NotNull(added);

        // 4. Delete tweak
        await vm.DeleteTweakCommand.ExecuteAsync(added);
        Assert.DoesNotContain(vm.CustomTweaks, t => t.Name == "Unit Test Valid Tweak");
    }

    [Fact]
    public void SettingsStorage_SaveAndLoad_PreservesSettings()
    {
        var settings = new AppSettings
        {
            Theme = "Light",
            AutoScanOnStartup = false,
            AdvancedMode = true,
            CreateRestorePointBeforeExecution = true,
            LogRetentionDays = 60
        };

        _settingsStorage.Save(settings);

        var loaded = _settingsStorage.Load();
        Assert.NotNull(loaded);
        Assert.Equal("Light", loaded.Theme);
        Assert.False(loaded.AutoScanOnStartup);
        Assert.True(loaded.AdvancedMode);
        Assert.Equal(60, loaded.LogRetentionDays);

        // Restore default Dark theme for subsequent runs
        settings.Theme = "Dark";
        _settingsStorage.Save(settings);
    }
}
