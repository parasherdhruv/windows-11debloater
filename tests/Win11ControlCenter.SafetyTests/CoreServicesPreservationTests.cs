using Win11ControlCenter.Core.Rules;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.SafetyTests;

public class CoreServicesPreservationTests
{
    [Theory]
    [InlineData("wuauserv")]
    [InlineData("WinDefend")]
    [InlineData("MpsSvc")]
    [InlineData("RpcSs")]
    [InlineData("CryptSvc")]
    [InlineData("Dhcp")]
    [InlineData("Dnscache")]
    [InlineData("EventLog")]
    [InlineData("Schedule")]
    public async Task CoreServices_CannotHaveStartupTypeAltered(string coreService)
    {
        var logger = new LoggingService();
        var serviceManager = new ServiceManagerService(logger);

        var result = await serviceManager.SetStartupTypeAsync(coreService, "Disabled");

        Assert.False(result, $"Safety shield must block modifying startup type of core service '{coreService}'.");
    }

    [Theory]
    [InlineData("RpcSs")]
    [InlineData("WinDefend")]
    [InlineData("EventLog")]
    [InlineData("Dnscache")]
    public async Task CoreServices_CannotBeStopped(string coreService)
    {
        var logger = new LoggingService();
        var serviceManager = new ServiceManagerService(logger);

        var result = await serviceManager.StopServiceAsync(coreService);

        Assert.False(result, $"Safety shield must block stopping core service '{coreService}'.");
    }
}

