using System.IO;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly ISystemDiscoveryService _discoveryService;
    private SystemInfo _systemInfo = new();

    public SystemInfo SystemInfo
    {
        get => _systemInfo;
        set => SetProperty(ref _systemInfo, value);
    }

    public DashboardViewModel(ILoggingService logger, ISystemDiscoveryService discoveryService)
    {
        _logger = logger;
        _discoveryService = discoveryService;
        ScanSystemCommand = new AsyncRelayCommand(ScanSystemAsync);
    }

    public IAsyncRelayCommand ScanSystemCommand { get; }

    public override async Task InitializeAsync()
    {
        await ScanSystemAsync();
    }

    private async Task ScanSystemAsync()
    {
        IsBusy = true;
        StatusMessage = "Performing read-only system scan...";
        _logger.LogInfo("Starting system scan", "Dashboard");

        try
        {
            // Genuine System Audit using SystemDiscoveryService
            var info = await _discoveryService.GetSystemInfoAsync();
            var (cpuPercent, availRam) = await _discoveryService.GetLiveResourceMetricsAsync();

            info.CpuUsagePercent = cpuPercent;
            info.AvailableRamGigabytes = availRam;

            SystemInfo = info;
            StatusMessage = $"Scan completed at {DateTime.Now:T}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
            _logger.LogError("Dashboard scan failure", ex, "Dashboard");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
