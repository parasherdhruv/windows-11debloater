using Win11ControlCenter.App.ViewModels;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class AppDebloaterWorkflowTests
{
    [Fact]
    public async Task DebloaterViewModel_SelectAllSafe_OnlySelectsSafeRemovablePackages()
    {
        var logger = new LoggingService();
        var backupRepo = new BackupRepository();
        var executor = new OperationExecutor(logger, backupRepo);
        var pkgManager = new PackageManagerService(logger, executor);

        var vm = new AppDebloaterViewModel(logger, pkgManager, executor);

        // Seed with test packages
        vm.Packages.Add(new ApplicationPackage
        {
            Name = "Microsoft.BingNews",
            DisplayName = "Microsoft News",
            RiskLevel = OperationRiskLevel.Safe,
            IsProtected = false
        });

        vm.Packages.Add(new ApplicationPackage
        {
            Name = "Microsoft.WindowsStore",
            DisplayName = "Microsoft Store",
            RiskLevel = OperationRiskLevel.Critical,
            IsProtected = true
        });

        vm.Packages.Add(new ApplicationPackage
        {
            Name = "Microsoft.YourPhone",
            DisplayName = "Phone Link",
            RiskLevel = OperationRiskLevel.Moderate,
            IsProtected = false
        });

        // Trigger Select All Safe
        vm.SelectAllSafeCommand.Execute(null);

        var bingNews = vm.Packages.First(p => p.Name == "Microsoft.BingNews");
        var store = vm.Packages.First(p => p.Name == "Microsoft.WindowsStore");
        var phone = vm.Packages.First(p => p.Name == "Microsoft.YourPhone");

        Assert.True(bingNews.IsSelectedForRemoval, "Safe app should be selected.");
        Assert.False(store.IsSelectedForRemoval, "Protected store app should never be selected.");
        Assert.False(phone.IsSelectedForRemoval, "Moderate risk app should not be selected by SelectAllSafe.");
        Assert.Equal(1, vm.SelectedCount);
    }

    [Fact]
    public async Task DebloaterViewModel_PreviewRemovalPlan_OpensModalWithItems()
    {
        var logger = new LoggingService();
        var backupRepo = new BackupRepository();
        var executor = new OperationExecutor(logger, backupRepo);
        var pkgManager = new PackageManagerService(logger, executor);

        var vm = new AppDebloaterViewModel(logger, pkgManager, executor);

        vm.Packages.Add(new ApplicationPackage
        {
            Name = "Microsoft.MicrosoftSolitaireCollection",
            DisplayName = "Solitaire Collection",
            PackageFullName = "Microsoft.MicrosoftSolitaireCollection_123_x64",
            RiskLevel = OperationRiskLevel.Safe,
            IsProtected = false,
            IsSelectedForRemoval = true
        });

        await vm.PreviewRemovalPlanCommand.ExecuteAsync(null);

        Assert.True(vm.IsPlanModalOpen);
        Assert.NotNull(vm.ActivePlan);
        Assert.Single(vm.ActivePlan.Items);
        Assert.Equal(OperationType.PackageRemoval, vm.ActivePlan.Items[0].OperationType);
        Assert.Equal(ReversibilityTier.ReinstallRequired, vm.ActivePlan.Items[0].Reversibility);
    }

    [Fact]
    public async Task DebloaterViewModel_QuickDebloat_SelectsSafeAndOpensModal()
    {
        var logger = new LoggingService();
        var backupRepo = new BackupRepository();
        var executor = new OperationExecutor(logger, backupRepo);
        var pkgManager = new PackageManagerService(logger, executor);

        var vm = new AppDebloaterViewModel(logger, pkgManager, executor);

        vm.Packages.Add(new ApplicationPackage
        {
            Name = "Microsoft.BingNews",
            DisplayName = "Microsoft News",
            RiskLevel = OperationRiskLevel.Safe,
            IsProtected = false
        });

        await vm.QuickDebloatCommand.ExecuteAsync(null);

        Assert.True(vm.IsPlanModalOpen);
        Assert.NotNull(vm.ActivePlan);
        Assert.Single(vm.ActivePlan.Items);
        Assert.Equal(1, vm.SelectedCount);
    }
}

