using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Xunit;

namespace Win11ControlCenter.SafetyTests;

public class ProtectedPackagesSafetyTests
{
    [Theory]
    [InlineData("Microsoft.Windows.ShellExperienceHost")]
    [InlineData("Microsoft.Windows.StartMenuExperienceHost")]
    [InlineData("Microsoft.Windows.Search")]
    [InlineData("Microsoft.Windows.SecHealthUI")]
    [InlineData("Microsoft.WindowsStore")]
    [InlineData("Microsoft.DesktopAppInstaller")]
    [InlineData("Microsoft.VCLibs.140.00")]
    [InlineData("Microsoft.UI.Xaml.2.8")]
    [InlineData("Microsoft.NET.Native.Runtime.2.2")]
    public void ProtectedPackages_AreCorrectlyIdentified(string packageName)
    {
        Assert.True(ProtectedPackages.IsProtected(packageName), $"Expected '{packageName}' to be marked as protected.");
    }

    [Fact]
    public void SafetyValidator_RejectsRemovalOfProtectedPackage()
    {
        var item = new OperationItem
        {
            Name = "Remove Windows Store",
            OperationType = OperationType.PackageRemoval,
            Target = "Microsoft.WindowsStore",
            RiskLevel = OperationRiskLevel.Critical
        };

        var (isValid, reason) = SafetyValidator.ValidateOperation(item);

        Assert.False(isValid);
        Assert.Contains("protected Windows system component", reason);
    }

    [Fact]
    public void SafetyValidator_AllowsRemovalOfSafeConsumerApp()
    {
        var item = new OperationItem
        {
            Name = "Remove Solitaire",
            OperationType = OperationType.PackageRemoval,
            Target = "Microsoft.MicrosoftSolitaireCollection",
            RiskLevel = OperationRiskLevel.Safe
        };

        var (isValid, reason) = SafetyValidator.ValidateOperation(item);

        Assert.True(isValid);
        Assert.Equal("Valid", reason);
    }
}

