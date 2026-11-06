using Win11ControlCenter.App.ViewModels;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class SelectionSurfacingAndSoftwareCenterTests
{
    [Fact]
    public void AppDebloater_SelectingPackage_SurfacesInSelectedCollectionImmediately()
    {
        var logger = new LoggingService();
        var backupRepo = new BackupRepository();
        var executor = new OperationExecutor(logger, backupRepo);
        var pkgManager = new PackageManagerService(logger, executor);
        var vm = new AppDebloaterViewModel(logger, pkgManager, executor);

        var pkg1 = new ApplicationPackage { Name = "App.One", DisplayName = "App One", RiskLevel = OperationRiskLevel.Safe };
        var pkg2 = new ApplicationPackage { Name = "App.Two", DisplayName = "App Two", RiskLevel = OperationRiskLevel.Safe };

        vm.Packages.Add(pkg1);
        vm.Packages.Add(pkg2);

        Assert.Empty(vm.SelectedPackages);
        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.HasSelectedPackages);

        // Select pkg1
        pkg1.IsSelectedForRemoval = true;

        Assert.Single(vm.SelectedPackages);
        Assert.Equal("App One", vm.SelectedPackages[0].DisplayName);
        Assert.Equal(1, vm.SelectedCount);
        Assert.True(vm.HasSelectedPackages);

        // Unselect via command
        vm.UnselectPackageCommand.Execute(pkg1);
        Assert.False(pkg1.IsSelectedForRemoval);
        Assert.Empty(vm.SelectedPackages);
        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.HasSelectedPackages);
    }

    [Fact]
    public void SystemCleaner_SelectedTargets_AutomaticallySynchronizeAndRecalculateTotals()
    {
        var logger = new LoggingService();
        var backupRepo = new BackupRepository();
        var executor = new OperationExecutor(logger, backupRepo);
        var cleanupService = new CleanupService(logger, executor);
        var vm = new SystemCleanerViewModel(logger, cleanupService, executor);

        var target1 = new CleanupTarget { Id = "T1", Name = "Target 1", FileCount = 10, TotalSizeBytes = 1024 * 1024 * 50, IsSelected = false };
        var target2 = new CleanupTarget { Id = "T2", Name = "Target 2", FileCount = 5, TotalSizeBytes = 1024 * 1024 * 20, IsSelected = false };

        vm.Targets.Add(target1);
        vm.Targets.Add(target2);

        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.HasSelectedTargets);
        Assert.Equal(0, vm.TotalSelectedBytes);

        // Select target1
        target1.IsSelected = true;

        Assert.Equal(1, vm.SelectedCount);
        Assert.True(vm.HasSelectedTargets);
        Assert.Equal(1024 * 1024 * 50, vm.TotalSelectedBytes);
        Assert.Equal(10, vm.TotalSelectedFiles);
        Assert.Single(vm.SelectedTargets);
        Assert.Equal("Target 1", vm.SelectedTargets[0].Name);

        // Unselect via command
        vm.UnselectTargetCommand.Execute(target1);
        Assert.False(target1.IsSelected);
        Assert.Equal(0, vm.SelectedCount);
        Assert.Empty(vm.SelectedTargets);
    }

    [Fact]
    public async Task SoftwareCenterViewModel_FiltersByCategoryAndSelectsSoftware()
    {
        var logger = new LoggingService();
        var wingetService = new WingetService(logger);
        var vm = new SoftwareCenterViewModel(wingetService, logger);

        await vm.InitializeAsync();

        Assert.True(vm.Packages.Count > 0);

        // Filter to Browsers
        vm.SelectedCategory = "Browsers";
        Assert.All(vm.Packages, p => Assert.Equal("Browsers", p.Category));

        // Select a browser
        var browser = vm.Packages.First();
        browser.IsSelected = true;

        Assert.Equal(1, vm.SelectedCount);
        Assert.True(vm.HasSelectedPackages);
        Assert.Single(vm.SelectedPackages);

        // Clear selection
        vm.ClearSelectionCommand.Execute(null);
        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.HasSelectedPackages);
        Assert.Empty(vm.SelectedPackages);
    }
}

