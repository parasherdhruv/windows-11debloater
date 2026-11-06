using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class SystemDiscoveryTests
{
    [Fact]
    public async Task GetSystemInfoAsync_ReturnsGenuineSystemMetrics()
    {
        var logger = new LoggingService();
        var discoveryService = new SystemDiscoveryService(logger);

        var info = await discoveryService.GetSystemInfoAsync();

        Assert.NotNull(info);
        Assert.False(string.IsNullOrWhiteSpace(info.OsName), "OS Name should not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(info.OsBuild), "OS Build should not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(info.CpuModel), "CPU Model should not be empty.");
        Assert.True(info.TotalRamGigabytes > 0, "Total RAM should be greater than zero.");
        Assert.True(info.SystemDriveTotalGigabytes > 0, "System drive total size should be greater than zero.");
        Assert.Contains("bit", info.Architecture, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsRunningAsAdministrator_ReturnsBooleanWithoutExceptions()
    {
        var logger = new LoggingService();
        var discoveryService = new SystemDiscoveryService(logger);

        var isAdmin = discoveryService.IsRunningAsAdministrator();
        // Should execute and return true or false cleanly
        Assert.True(isAdmin || !isAdmin);
    }

    [Fact]
    public void IsPendingReboot_ReturnsBooleanWithoutExceptions()
    {
        var logger = new LoggingService();
        var discoveryService = new SystemDiscoveryService(logger);

        var isPending = discoveryService.IsPendingReboot();
        Assert.True(isPending || !isPending);
    }
}

