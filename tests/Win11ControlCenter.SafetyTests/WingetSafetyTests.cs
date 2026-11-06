using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.SafetyTests;

public class WingetSafetyTests
{
    [Fact]
    public async Task CuratedPackages_AllIdsMatchStrictSafetyRegex()
    {
        var logger = new LoggingService();
        var wingetService = new WingetService(logger);

        var packages = await wingetService.GetCuratedPackagesAsync();
        Assert.NotEmpty(packages);

        foreach (var pkg in packages)
        {
            Assert.Matches(@"^[a-zA-Z0-9\.\-_+]+$", pkg.Id);
            Assert.DoesNotContain(";", pkg.Id);
            Assert.DoesNotContain("&", pkg.Id);
            Assert.DoesNotContain("|", pkg.Id);
            Assert.DoesNotContain("\"", pkg.Id);
            Assert.DoesNotContain("'", pkg.Id);
            Assert.DoesNotContain(" ", pkg.Id);
            Assert.False(string.IsNullOrWhiteSpace(pkg.Name));
            Assert.False(string.IsNullOrWhiteSpace(pkg.Category));
            Assert.False(string.IsNullOrWhiteSpace(pkg.Publisher));
        }
    }

    [Theory]
    [InlineData("malicious;calc.exe")]
    [InlineData("7zip.7zip | format C:")]
    [InlineData("evil&&powershell")]
    [InlineData("bad package with spaces")]
    [InlineData("`rmdir /s /q`")]
    public async Task InstallPackage_RejectsMaliciousPackageIds(string invalidId)
    {
        var logger = new LoggingService();
        var wingetService = new WingetService(logger);

        var badPkg = new Core.Models.SoftwarePackageItem
        {
            Id = invalidId,
            Name = "Untrusted Package"
        };

        var result = await wingetService.InstallPackageAsync(badPkg);
        Assert.False(result);
        Assert.Equal("Error", badPkg.InstallStatus);
        Assert.Equal("Invalid package identifier.", badPkg.StatusMessage);
    }
}
