using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.Core.Interfaces;

public interface ISystemDiscoveryService
{
    Task<SystemInfo> GetSystemInfoAsync(CancellationToken cancellationToken = default);
    Task<(double CpuPercent, double AvailableRamGb)> GetLiveResourceMetricsAsync(CancellationToken cancellationToken = default);
    bool IsRunningAsAdministrator();
    bool IsPendingReboot();
}

