using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.Core.Interfaces;

public interface IWingetService
{
    Task<bool> IsWingetAvailableAsync();
    Task<List<SoftwarePackageItem>> GetCuratedPackagesAsync();
    Task RefreshInstalledStatusAsync(IEnumerable<SoftwarePackageItem> packages);
    Task<bool> InstallPackageAsync(SoftwarePackageItem package, IProgress<string>? progress = null, CancellationToken ct = default);
}
