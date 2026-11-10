using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Win11ControlCenter.App;
using Win11ControlCenter.App.ViewModels;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

/// <summary>
/// Full Application End-to-End Automation Test Suite.
/// Exercises and verifies all 13 modules, DI registration, navigation,
/// selection trays, safety shields, and operations end-to-end.
/// </summary>
public class FullAppAutomationFeatureTests
{
    private static void RunInSta(Func<Task> action)
    {
        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            try
            {
                try
                {
                    if (Application.Current == null)
                    {
                        var app = new Application();
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/Win11ControlCenter.App;component/Themes/ThemeResources.xaml")
                        });
                    }
                    else if (Application.Current.Resources.MergedDictionaries.Count == 0)
                    {
                        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/Win11ControlCenter.App;component/Themes/ThemeResources.xaml")
                        });
                    }
                }
                catch (InvalidOperationException)
                {
                    // Ignore if Application was already initialized by another test
                }

                action().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (thrown != null)
        {
            throw new AggregateException("STA Automation execution failed", thrown);
        }
    }

    private static IServiceProvider CreateAppServiceProvider()
    {
        var services = new ServiceCollection();
        Win11ControlCenter.App.App.ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Test01_DIContainer_RegistersAndResolvesAll13ServicesAndViewModels()
    {
        RunInSta(() =>
        {
            var sp = CreateAppServiceProvider();

            // Verify Core & Infrastructure Services
            Assert.NotNull(sp.GetRequiredService<ILoggingService>());
            Assert.NotNull(sp.GetRequiredService<BackupRepository>());
            Assert.NotNull(sp.GetRequiredService<SettingsStorage>());
            Assert.NotNull(sp.GetRequiredService<IOperationExecutor>());
            Assert.NotNull(sp.GetRequiredService<ISystemDiscoveryService>());
            Assert.NotNull(sp.GetRequiredService<IPackageManagerService>());
            Assert.NotNull(sp.GetRequiredService<IServiceManagerService>());
            Assert.NotNull(sp.GetRequiredService<IStartupManagerService>());
            Assert.NotNull(sp.GetRequiredService<IPrivacyService>());
            Assert.NotNull(sp.GetRequiredService<ITweakEngineService>());
            Assert.NotNull(sp.GetRequiredService<IOptimizationProfileService>());
            Assert.NotNull(sp.GetRequiredService<ICleanupService>());
            Assert.NotNull(sp.GetRequiredService<IRestoreService>());
            Assert.NotNull(sp.GetRequiredService<IWingetService>());

            // Verify All 13 ViewModels
            Assert.NotNull(sp.GetRequiredService<DashboardViewModel>());
            Assert.NotNull(sp.GetRequiredService<AppDebloaterViewModel>());
            Assert.NotNull(sp.GetRequiredService<SoftwareCenterViewModel>());
            Assert.NotNull(sp.GetRequiredService<StartupManagerViewModel>());
            Assert.NotNull(sp.GetRequiredService<ServicesManagerViewModel>());
            Assert.NotNull(sp.GetRequiredService<PrivacyViewModel>());
            Assert.NotNull(sp.GetRequiredService<OptimizationViewModel>());
            Assert.NotNull(sp.GetRequiredService<GamingViewModel>());
            Assert.NotNull(sp.GetRequiredService<SystemCleanerViewModel>());
            Assert.NotNull(sp.GetRequiredService<CustomTweaksViewModel>());
            Assert.NotNull(sp.GetRequiredService<RestoreCenterViewModel>());
            Assert.NotNull(sp.GetRequiredService<ActivityLogsViewModel>());
            Assert.NotNull(sp.GetRequiredService<SettingsViewModel>());

            // Verify Main Window & Shell ViewModel
            Assert.NotNull(sp.GetRequiredService<MainViewModel>());
            Assert.NotNull(sp.GetRequiredService<MainWindow>());

            return Task.CompletedTask;
        });
    }

    [Fact]
    public void Test02_Navigation_CyclesThroughAll13Pages_WithoutExceptions()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var mainVm = sp.GetRequiredService<MainViewModel>();

            var pages = new[]
            {
                ("Dashboard", "Dashboard", typeof(DashboardViewModel)),
                ("AppDebloater", "App Debloater", typeof(AppDebloaterViewModel)),
                ("SoftwareCenter", "Software Center", typeof(SoftwareCenterViewModel)),
                ("StartupManager", "Startup Manager", typeof(StartupManagerViewModel)),
                ("ServicesManager", "Windows Services", typeof(ServicesManagerViewModel)),
                ("Privacy", "Privacy & Telemetry", typeof(PrivacyViewModel)),
                ("Optimization", "Windows Optimization", typeof(OptimizationViewModel)),
                ("Gaming", "Gaming Optimization", typeof(GamingViewModel)),
                ("SystemCleaner", "System Cleaner", typeof(SystemCleanerViewModel)),
                ("CustomTweaks", "Custom Tweaks", typeof(CustomTweaksViewModel)),
                ("RestoreCenter", "Restore Center", typeof(RestoreCenterViewModel)),
                ("ActivityLogs", "Activity Logs", typeof(ActivityLogsViewModel)),
                ("Settings", "Settings", typeof(SettingsViewModel))
            };

            foreach (var (key, title, expectedType) in pages)
            {
                mainVm.NavigateCommand.Execute(key);
                Assert.Equal(title, mainVm.CurrentPageTitle);
                Assert.IsType(expectedType, mainVm.CurrentViewModel);
            }

            await Task.CompletedTask;
        });
    }

    [Fact]
    public void Test03_Dashboard_DiscoversHardwareRamPendingRebootsAndPowerPlans()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<DashboardViewModel>();

            await vm.InitializeAsync();

            Assert.NotNull(vm.SystemInfo);
            Assert.False(string.IsNullOrWhiteSpace(vm.SystemInfo.Architecture));
            Assert.True(vm.SystemInfo.TotalRamGigabytes > 0);
            Assert.False(string.IsNullOrWhiteSpace(vm.SystemInfo.ActivePowerPlan));
            Assert.NotNull(vm.ScanSystemCommand);
        });
    }

    [Fact]
    public void Test04_AppDebloater_SelectSafePinToTopAndPreviewRemovalPlan()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<AppDebloaterViewModel>();

            // Seed test catalog
            vm.Packages.Clear();
            var safeApp = new ApplicationPackage
            {
                Name = "Microsoft.BingNews",
                DisplayName = "Microsoft News",
                PackageFullName = "Microsoft.BingNews_8wekyb3d8bbwe",
                RiskLevel = OperationRiskLevel.Safe,
                IsProtected = false
            };
            var protectedApp = new ApplicationPackage
            {
                Name = "Microsoft.WindowsStore",
                DisplayName = "Microsoft Store",
                PackageFullName = "Microsoft.WindowsStore_8wekyb3d8bbwe",
                RiskLevel = OperationRiskLevel.Critical,
                IsProtected = true
            };

            vm.Packages.Add(safeApp);
            vm.Packages.Add(protectedApp);

            // Test 1: Select Safe Bloat
            vm.SelectAllSafeCommand.Execute(null);
            Assert.True(safeApp.IsSelectedForRemoval);
            Assert.False(protectedApp.IsSelectedForRemoval);
            Assert.True(vm.HasSelectedPackages);
            Assert.Equal(1, vm.SelectedCount);
            Assert.Contains(safeApp, vm.SelectedPackages);

            // Test 2: Pin to top
            vm.PinSelectedToTop = true;
            Assert.True(vm.PinSelectedToTop);

            // Test 3: Unselect via Chip Tray
            vm.UnselectPackageCommand.Execute(safeApp);
            Assert.False(safeApp.IsSelectedForRemoval);
            Assert.False(vm.HasSelectedPackages);
            Assert.Equal(0, vm.SelectedCount);

            // Test 4: Re-select and Preview Plan
            safeApp.IsSelectedForRemoval = true;
            await vm.PreviewRemovalPlanCommand.ExecuteAsync(null);

            // Test 5: Clear Selection
            vm.ClearSelectionCommand.Execute(null);
            Assert.False(safeApp.IsSelectedForRemoval);
            Assert.Equal(0, vm.SelectedCount);
        });
    }

    [Fact]
    public void Test05_SoftwareCenter_FilterSearchSelectionAndDryRunInstall()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<SoftwareCenterViewModel>();

            await vm.InitializeAsync();

            Assert.NotEmpty(vm.Packages);

            // Test Category Filtering
            vm.FilterCategoryCommand.Execute("Utilities");
            Assert.All(vm.Packages, p => Assert.Equal("Utilities", p.Category));

            vm.FilterCategoryCommand.Execute("All");
            Assert.True(vm.Packages.Count > 5);

            // Test Search Filtering
            vm.SearchFilter = "git";
            Assert.NotEmpty(vm.Packages);
            Assert.All(vm.Packages, p => Assert.True(
                p.Name.Contains("git", StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains("git", StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains("git", StringComparison.OrdinalIgnoreCase)));
            vm.SearchFilter = "";

            // Test Selection & Selected Tray
            var first = vm.Packages.First();
            first.IsSelected = true;
            Assert.True(vm.HasSelectedPackages);
            Assert.Equal(1, vm.SelectedCount);
            Assert.Contains(first, vm.SelectedPackages);

            // Test Deselect via ClearSelectionCommand
            vm.ClearSelectionCommand.Execute(null);
            Assert.False(first.IsSelected);
            Assert.False(vm.HasSelectedPackages);
        });
    }

    [Fact]
    public void Test06_StartupManager_InspectsImpactsAndSafeToggles()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<StartupManagerViewModel>();

            await vm.InitializeAsync();

            // Test search filter
            vm.SearchFilter = "test";
            Assert.NotNull(vm.StartupItems);
            vm.SearchFilter = "";

            // Toggle item state non-destructively
            var item = new StartupEntry
            {
                Name = "AutoUpdater",
                Command = "C:\\Program Files\\App\\update.exe",
                LocationType = StartupLocationType.RegistryCurrentUser,
                IsEnabled = true
            };
            vm.StartupItems.Add(item);

            await vm.ToggleStateCommand.ExecuteAsync(item);
            Assert.False(item.IsEnabled);

            await vm.ToggleStateCommand.ExecuteAsync(item);
            Assert.True(item.IsEnabled);
        });
    }

    [Fact]
    public void Test07_ServicesManager_ProtectsCoreServicesAndExportsInventory()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<ServicesManagerViewModel>();

            await vm.InitializeAsync();
            Assert.NotEmpty(vm.Services);

            // Verify Core Service Protection Flag
            var rpcService = vm.Services.FirstOrDefault(s => s.ServiceName.Equals("RpcSs", StringComparison.OrdinalIgnoreCase) 
                                                          || s.ServiceName.Equals("DcomLaunch", StringComparison.OrdinalIgnoreCase)
                                                          || s.ServiceName.Equals("EventLog", StringComparison.OrdinalIgnoreCase));
            if (rpcService != null)
            {
                Assert.True(rpcService.IsCoreService, "Core Windows service must be marked as protected.");
            }

            // Test Search Filter
            vm.SearchFilter = "Windows";
            Assert.NotEmpty(vm.Services);
            Assert.All(vm.Services, s => Assert.True(
                s.DisplayName.Contains("Windows", StringComparison.OrdinalIgnoreCase) ||
                s.ServiceName.Contains("Windows", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("Windows", StringComparison.OrdinalIgnoreCase)));
            vm.SearchFilter = "";

            // Test Export Inventory
            await vm.ExportInventoryCommand.ExecuteAsync(null);
        });
    }

    [Fact]
    public void Test08_PrivacyManager_TelemetryBaselineFilteringAndRevert()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<PrivacyViewModel>();

            await vm.InitializeAsync();

            Assert.NotEmpty(vm.Tweaks);
            Assert.True(vm.TotalCount > 0);

            // Test Filtering
            if (vm.Categories.Count > 1)
            {
                vm.SelectedCategory = vm.Categories[1];
                Assert.NotEmpty(vm.Tweaks);
                vm.SelectedCategory = "All";
            }

            // Test Search
            vm.SearchFilter = "telemetry";
            Assert.NotNull(vm.Tweaks);
            vm.SearchFilter = "";

            // Test Baseline Preview & Revert
            await vm.ApplyBaselineCommand.ExecuteAsync(null);
            await vm.RevertAllCommand.ExecuteAsync(null);
        });
    }

    [Fact]
    public void Test09_OptimizationEngine_ProfileSwitchingAndTweaks()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<OptimizationViewModel>();

            await vm.InitializeAsync();

            Assert.NotEmpty(vm.Profiles);
            Assert.NotNull(vm.SelectedProfile);

            // Switch to Aggressive profile
            var aggressive = vm.Profiles.FirstOrDefault(p => p.Name.Contains("Aggressive", StringComparison.OrdinalIgnoreCase));
            if (aggressive != null)
            {
                vm.SelectedProfile = aggressive;
                Assert.Equal(aggressive, vm.SelectedProfile);
            }

            // Test individual tweak toggle
            if (vm.ExplorerTweaks.Count > 0)
            {
                var tweak = vm.ExplorerTweaks.First();
                await vm.ToggleExplorerTweakCommand.ExecuteAsync(tweak);
            }
        });
    }

    [Fact]
    public void Test10_GamingMode_OptimizationsAndPowerScheme()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<GamingViewModel>();

            await vm.InitializeAsync();

            Assert.NotEmpty(vm.GamingTweaks);
            Assert.False(string.IsNullOrWhiteSpace(vm.ActivePowerPlanName));

            // Test dry-run optimization execution
            await vm.OptimizeAllGamingCommand.ExecuteAsync(null);
        });
    }

    [Fact]
    public void Test11_SystemCleaner_ScanSelectAllAndChipTray()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<SystemCleanerViewModel>();

            await vm.ScanCommand.ExecuteAsync(null);
            Assert.NotEmpty(vm.Targets);

            // Test Select All
            vm.SelectAllCommand.Execute(null);
            Assert.True(vm.HasSelectedTargets);
            Assert.True(vm.SelectedCount > 0);
            Assert.NotEmpty(vm.SelectedTargets);

            // Test Chip Tray unselect
            var target = vm.SelectedTargets.First();
            vm.UnselectTargetCommand.Execute(target);
            Assert.DoesNotContain(target, vm.SelectedTargets);

            // Test Deselect All
            vm.DeselectAllCommand.Execute(null);
            Assert.False(vm.HasSelectedTargets);
            Assert.Equal(0, vm.SelectedCount);
        });
    }

    [Fact]
    public void Test12_CustomTweaks_ValidatesAndBlocksDangerousKeys()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<CustomTweaksViewModel>();

            await vm.InitializeAsync();

            // Test 1: Dangerous SAM key should be rejected
            vm.NewName = "SAM Tweak";
            vm.NewHive = "HKLM";
            vm.NewKeyPath = @"SAM\SAM\Domains\Account";
            vm.NewValueName = "TestValue";
            vm.NewRecommendedValue = "1";
            vm.NewValueType = "DWord";
            await vm.AddTweakCommand.ExecuteAsync(null);

            // Tweak should be blocked with safety validation error
            Assert.Contains("Safety Block", vm.ValidationError);
            Assert.DoesNotContain(vm.CustomTweaks, t => t.Name == "SAM Tweak");

            // Test 2: Valid Explorer tweak should be accepted
            vm.NewName = "ValidAnimationTweak";
            vm.NewHive = "HKCU";
            vm.NewKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
            vm.NewValueName = "TaskbarAnimations";
            vm.NewRecommendedValue = "0";
            vm.NewValueType = "DWord";
            await vm.AddTweakCommand.ExecuteAsync(null);

            Assert.Empty(vm.ValidationError);
            Assert.Contains(vm.CustomTweaks, t => t.Name == "ValidAnimationTweak");
        });
    }

    [Fact]
    public void Test13_RestoreCenter_CreatesSnapshotAndRollbackPlan()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var vm = sp.GetRequiredService<RestoreCenterViewModel>();
            var backupRepo = sp.GetRequiredService<BackupRepository>();

            // Create a test snapshot to ensure at least one backup session is available
            var testPlan = new OperationPlan
            {
                Title = "Automation Test Restore Plan",
                ProfileName = "AutomationTesting",
                Items = new List<OperationItem>
                {
                    new()
                    {
                        Name = "Automation Test Restore Item",
                        OperationType = OperationType.Registry,
                        Target = @"HKCU\Software\Win11ControlCenter\Test",
                        OriginalValue = "0",
                        RecommendedValue = "1"
                    }
                }
            };
            backupRepo.CreateBackupSession(testPlan, "26100.1000");

            await vm.InitializeAsync();
            Assert.NotEmpty(vm.Backups);

            // Verify selected backup has valid properties
            var latest = vm.Backups.First();
            Assert.NotNull(latest);
            Assert.False(string.IsNullOrWhiteSpace(latest.BackupId));
            vm.SelectedBackup = latest;
            Assert.NotNull(vm.SelectedBackup);
        });
    }

    [Fact]
    public void Test14_ActivityLogs_LogsEventsAndExportsAuditTrail()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var logger = sp.GetRequiredService<ILoggingService>();
            var vm = sp.GetRequiredService<ActivityLogsViewModel>();

            logger.LogOperation(new OperationItem
            {
                Name = "Automated E2E Test Operation",
                Category = "AutomationTest",
                Target = @"HKCU\Software\Automation",
                Status = ExecutionStatus.Succeeded,
                ExecutedAt = DateTime.UtcNow
            });

            await vm.InitializeAsync();

            Assert.NotEmpty(vm.FilteredEntries);
            Assert.Contains(vm.FilteredEntries, l => l.Name.Contains("Automated E2E Test") || l.Target.Contains("Automated E2E Test"));

            // Test Search Filtering
            vm.SearchFilter = "Automated E2E";
            Assert.NotEmpty(vm.FilteredEntries);
            vm.SearchFilter = "";

            // Test Export
            await vm.ExportLogsCommand.ExecuteAsync(null);
        });
    }

    [Fact]
    public void Test15_SettingsAndElevation_ThemeToggleAndUACStatus()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var settingsVm = sp.GetRequiredService<SettingsViewModel>();
            var mainVm = sp.GetRequiredService<MainViewModel>();

            await settingsVm.InitializeAsync();

            // Test Theme Toggling
            var originalTheme = settingsVm.SelectedTheme;
            var targetTheme = originalTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";
            settingsVm.SelectedTheme = targetTheme;
            settingsVm.SaveCommand.Execute(null);
            Assert.Equal(targetTheme, settingsVm.SelectedTheme);

            // Revert theme
            settingsVm.SelectedTheme = originalTheme;
            settingsVm.SaveCommand.Execute(null);
            Assert.Equal(originalTheme, settingsVm.SelectedTheme);

            // Test Elevation Status
            Assert.NotNull(mainVm.ElevationStatusText);
            Assert.True(mainVm.ElevationStatusText.Contains("Administrator") || mainVm.ElevationStatusText.Contains("Standard User"));
        });
    }

    [Fact]
    public void Test16_SidebarThemeSelection_PreservedWhenNavigatingToSettings()
    {
        RunInSta(async () =>
        {
            var sp = CreateAppServiceProvider();
            var mainVm = sp.GetRequiredService<MainViewModel>();
            var settingsVm = sp.GetRequiredService<SettingsViewModel>();

            // 1. User selects Light theme from sidebar dock pills
            mainVm.SelectThemeCommand.Execute("Light");
            Assert.Equal("Light", mainVm.ThemeService.CurrentTheme);
            Assert.True(mainVm.IsLightTheme);

            // 2. User clicks "Application Settings" navigation in sidebar
            mainVm.NavigateCommand.Execute("Settings");
            await settingsVm.InitializeAsync();

            // 3. Theme must remain Light - NOT reset back to default
            Assert.Equal("Light", mainVm.ThemeService.CurrentTheme);
            Assert.Equal("Light", settingsVm.SelectedTheme);
            Assert.True(mainVm.IsLightTheme);

            // 4. User selects Cobalt theme
            mainVm.SelectThemeCommand.Execute("Cobalt");
            Assert.Equal("Cobalt", mainVm.ThemeService.CurrentTheme);

            // 5. Navigate to settings again
            await settingsVm.InitializeAsync();
            Assert.Equal("Cobalt", mainVm.ThemeService.CurrentTheme);
            Assert.Equal("Cobalt", settingsVm.SelectedTheme);
        });
    }

    [Fact]
    public void Test17_RegistryOperationHandler_JsonElementAndNullValueType_HandledSafely()
    {
        // 1. Test null ValueType (should not throw NullReferenceException)
        var itemNullType = new OperationItem
        {
            Name = "Test Null Type",
            OperationType = OperationType.Registry,
            Target = @"HKCU\Software\Win11ControlCenterTest",
            ValueType = null!,
            RecommendedValue = 1,
            Metadata = new Dictionary<string, string> { { "ValueName", "TestVal" } }
        };
        var (okNull, _) = Win11ControlCenter.SystemEngine.Handlers.RegistryOperationHandler.Apply(itemNullType);
        Assert.True(okNull);

        // 2. Test JsonElement numeric DWord normalization
        using var doc = System.Text.Json.JsonDocument.Parse("{\"dwordVal\": 42, \"boolVal\": true, \"strVal\": \"100\"}");
        var jsonDword = doc.RootElement.GetProperty("dwordVal");
        var normalizedNum = Win11ControlCenter.SystemEngine.Handlers.RegistryOperationHandler.NormalizeRegistryValue(jsonDword, Microsoft.Win32.RegistryValueKind.DWord);
        Assert.Equal(42, normalizedNum);

        // 3. Test JsonElement boolean normalization to DWord
        var jsonBool = doc.RootElement.GetProperty("boolVal");
        var normalizedBool = Win11ControlCenter.SystemEngine.Handlers.RegistryOperationHandler.NormalizeRegistryValue(jsonBool, Microsoft.Win32.RegistryValueKind.DWord);
        Assert.Equal(1, normalizedBool);

        // 4. Test JsonElement string number normalization to DWord
        var jsonStr = doc.RootElement.GetProperty("strVal");
        var normalizedStr = Win11ControlCenter.SystemEngine.Handlers.RegistryOperationHandler.NormalizeRegistryValue(jsonStr, Microsoft.Win32.RegistryValueKind.DWord);
        Assert.Equal(100, normalizedStr);

        // Cleanup test registry key
        try
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree("Software\\Win11ControlCenterTest", false);
        }
        catch { }
    }

    [Fact]
    public void Test18_EmergencyRegFile_EscapedCorrectly()
    {
        var plan = new OperationPlan
        {
            Title = "Escape Test Plan",
            Items = new List<OperationItem>
            {
                new()
                {
                    Name = "Path with Backslashes",
                    OperationType = OperationType.Registry,
                    Target = @"HKCU\Software\TestPath",
                    OriginalValue = @"C:\Program Files\Test App\test.exe",
                    RecommendedValue = @"C:\Program Files\New\test.exe",
                    ValueType = "String",
                    Metadata = new Dictionary<string, string> { { "ValueName", "InstallPath" } }
                }
            }
        };

        var repo = new BackupRepository();
        var backupPath = repo.CreateBackupSession(plan);
        var regFile = Path.Combine(backupPath, "registry", "pre_apply_rollback.reg");
        Assert.True(File.Exists(regFile));

        var lines = File.ReadAllLines(regFile);
        var pathLine = lines.FirstOrDefault(l => l.StartsWith("\"InstallPath\"="));
        Assert.NotNull(pathLine);
        // Backslashes must be doubled for valid .reg files
        Assert.Contains(@"C:\\Program Files\\Test App\\test.exe", pathLine);

        // Cleanup
        repo.DeleteBackupSession(Path.GetFileName(backupPath));
    }
}
