using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class PackageManagerTests
{
    [Fact]
    public async Task CreateRemovalPlanAsync_ExcludesProtectedPackages()
    {
        var logger = new LoggingService();
        var backupRepo = new BackupRepository();
        var executor = new OperationExecutor(logger, backupRepo);
        var pkgManager = new PackageManagerService(logger, executor);

        var packages = new List<ApplicationPackage>
        {
            new()
            {
                Name = "Microsoft.WindowsStore",
                DisplayName = "Microsoft Store",
                PackageFullName = "Microsoft.WindowsStore_123_x64",
                IsProtected = true,
                IsSelectedForRemoval = true
            },
            new()
            {
                Name = "Microsoft.BingWeather",
                DisplayName = "MSN Weather",
                PackageFullName = "Microsoft.BingWeather_456_x64",
                IsProtected = false,
                IsSelectedForRemoval = true,
                RiskLevel = OperationRiskLevel.Safe
            }
        };

        var plan = await pkgManager.CreateRemovalPlanAsync(packages, isDryRun: true);

        // Microsoft.WindowsStore must be completely rejected by the safety layer
        Assert.DoesNotContain(plan.Items, item => item.Target.Contains("WindowsStore", StringComparison.OrdinalIgnoreCase));
        // Safe app must be present in plan
        Assert.Contains(plan.Items, item => item.Target.Contains("BingWeather", StringComparison.OrdinalIgnoreCase));
    }
}

