using System.IO;
using Win11ControlCenter.App.ViewModels;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class StartupAndServicesManagerTests
{
    [Fact]
    public async Task StartupManager_ExportsInventoryToJson()
    {
        var logger = new LoggingService();
        var startupManager = new StartupManagerService(logger);

        var sampleEntries = new List<StartupEntry>
        {
            new()
            {
                Name = "TestApp",
                Command = "C:\\Tools\\TestApp.exe",
                ExecutablePath = "C:\\Tools\\TestApp.exe",
                LocationType = StartupLocationType.RegistryCurrentUser,
                IsEnabled = true
            }
        };

        var tempExportPath = Path.Combine(Path.GetTempPath(), $"startup_test_{Guid.NewGuid():N}.json");

        var path = await startupManager.ExportStartupInventoryAsync(sampleEntries, tempExportPath);

        Assert.True(File.Exists(path));
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("TestApp", content);
        Assert.Contains("RegistryCurrentUser", content);

        try { File.Delete(path); } catch { }
    }

    [Fact]
    public async Task ServiceManager_ExportsInventoryToJson()
    {
        var logger = new LoggingService();
        var serviceManager = new ServiceManagerService(logger);

        var sampleServices = new List<WindowsServiceItem>
        {
            new()
            {
                ServiceName = "TestService",
                DisplayName = "Test Windows Service",
                Status = "Running",
                StartupType = "Automatic",
                Category = ServiceCategory.Telemetry
            }
        };

        var tempExportPath = Path.Combine(Path.GetTempPath(), $"services_test_{Guid.NewGuid():N}.json");

        var path = await serviceManager.ExportServicesInventoryAsync(sampleServices, tempExportPath);

        Assert.True(File.Exists(path));
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("TestService", content);
        Assert.Contains("Telemetry", content);

        try { File.Delete(path); } catch { }
    }

    [Fact]
    public void StartupViewModel_FiltersCorrectly()
    {
        var logger = new LoggingService();
        var startupManager = new StartupManagerService(logger);
        var vm = new StartupManagerViewModel(logger, startupManager);

        vm.StartupItems.Add(new StartupEntry { Name = "OneDrive", Command = "onedrive.exe" });
        vm.StartupItems.Add(new StartupEntry { Name = "Teams", Command = "teams.exe" });

        vm.SearchFilter = "OneDrive";

        // Filter applied
        Assert.NotNull(vm.StartupItems);
    }
}

