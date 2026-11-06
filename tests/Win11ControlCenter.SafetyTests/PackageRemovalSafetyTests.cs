using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.SystemEngine.Handlers;
using Xunit;

namespace Win11ControlCenter.SafetyTests;

public class PackageRemovalSafetyTests
{
    [Theory]
    [InlineData("Microsoft.WindowsStore")]
    [InlineData("Microsoft.Windows.ShellExperienceHost")]
    [InlineData("Microsoft.Windows.StartMenuExperienceHost")]
    [InlineData("Microsoft.Windows.SecHealthUI")]
    [InlineData("Microsoft.DesktopAppInstaller")]
    [InlineData("Microsoft.VCLibs.140.00")]
    public async Task PackageRemoval_StrictlyRefusesProtectedPackages(string protectedTarget)
    {
        var item = new OperationItem
        {
            Name = $"Attempted removal of {protectedTarget}",
            OperationType = OperationType.PackageRemoval,
            Target = protectedTarget,
            RiskLevel = OperationRiskLevel.Critical
        };

        var (success, message) = await PackageRemovalOperationHandler.ApplyAsync(item);

        Assert.False(success);
        Assert.Contains("Safety shield active", message);
    }

    [Theory]
    [InlineData("validPackageName; Remove-Item C:\\ -Recurse")]
    [InlineData("pkg & calc.exe")]
    [InlineData("app | Out-File evil.ps1")]
    [InlineData("test`$evil")]
    [InlineData("package\nmalicious")]
    public async Task PackageRemoval_RejectsCommandInjectionPayloads(string maliciousPayload)
    {
        var item = new OperationItem
        {
            Name = "Injection Attempt",
            OperationType = OperationType.PackageRemoval,
            Target = maliciousPayload
        };

        var (success, message) = await PackageRemovalOperationHandler.ApplyAsync(item);

        Assert.False(success);
        Assert.Contains("Security rejection", message);
    }
}

