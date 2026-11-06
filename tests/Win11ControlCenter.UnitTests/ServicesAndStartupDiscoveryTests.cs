using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class ServicesAndStartupDiscoveryTests
{
    [Fact]
    public async Task GetServicesAsync_FindsWindowsServices()
    {
        var logger = new LoggingService();
        var serviceManager = new ServiceManagerService(logger);

        var services = await serviceManager.GetServicesAsync();

        Assert.NotEmpty(services);
        Assert.Contains(services, s => s.ServiceName.Equals("RpcSs", StringComparison.OrdinalIgnoreCase));
        var rpc = services.First(s => s.ServiceName.Equals("RpcSs", StringComparison.OrdinalIgnoreCase));
        Assert.True(rpc.IsCoreService, "RpcSs must be recognized as a core service.");
    }

    [Fact]
    public async Task ServiceManager_RefusesModifyingCoreService()
    {
        var logger = new LoggingService();
        var serviceManager = new ServiceManagerService(logger);

        var setSuccess = await serviceManager.SetStartupTypeAsync("RpcSs", "Disabled");
        Assert.False(setSuccess, "Must refuse to disable RpcSs.");

        var stopSuccess = await serviceManager.StopServiceAsync("RpcSs");
        Assert.False(stopSuccess, "Must refuse to stop RpcSs.");
    }

    [Fact]
    public async Task GetStartupEntriesAsync_ExecutesWithoutException()
    {
        var logger = new LoggingService();
        var startupManager = new StartupManagerService(logger);

        var entries = await startupManager.GetStartupEntriesAsync();
        Assert.NotNull(entries);
        // Can be empty or contain apps, but must execute safely
    }
}

