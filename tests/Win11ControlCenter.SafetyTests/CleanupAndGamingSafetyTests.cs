using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Win11ControlCenter.SystemEngine.Handlers;
using Win11ControlCenter.SystemEngine.Services;
using Xunit;

namespace Win11ControlCenter.SafetyTests;

public class CleanupAndGamingSafetyTests
{
    [Theory]
    [InlineData(@"C:\Windows\System32")]
    [InlineData(@"C:\Windows\WinSxS")]
    [InlineData(@"C:\Windows\Installer")]
    [InlineData(@"C:\Windows\Prefetch")]
    [InlineData(@"C:\Program Files")]
    [InlineData(@"C:\Program Files (x86)")]
    [InlineData(@"C:\Users\User\Documents")]
    [InlineData(@"C:\Users\User\Desktop")]
    [InlineData(@"C:\Users\User\Downloads")]
    [InlineData(@"C:\Users\User\Pictures")]
    [InlineData(@"C:\Users\User\Music")]
    [InlineData(@"C:\Users\User\Videos")]
    [InlineData(@"C:\")]
    [InlineData(@"D:\")]
    public void SafetyValidator_MustBlockProtectedDirectoriesFromCleanup(string forbiddenPath)
    {
        var item = new OperationItem
        {
            Name = "Dangerous Cleanup",
            OperationType = OperationType.FileCleanup,
            Target = forbiddenPath
        };

        var (isValid, reason) = SafetyValidator.ValidateOperation(item);

        Assert.False(isValid, $"SafetyValidator must block cleanup targeting protected directory: {forbiddenPath}");
        Assert.True(reason.Contains("protected", StringComparison.OrdinalIgnoreCase) || 
                    reason.Contains("root", StringComparison.OrdinalIgnoreCase) ||
                    reason.Contains("cannot be targeted", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(@"C:\Windows\System32")]
    [InlineData(@"C:\Users\User\Documents")]
    [InlineData(@"C:\")]
    public void CleanupOperationHandler_MustRefuseProtectedTargets(string forbiddenPath)
    {
        var item = new OperationItem
        {
            Name = "Blocked Item",
            OperationType = OperationType.FileCleanup,
            Target = forbiddenPath
        };

        var (success, message, bytes, files) = CleanupOperationHandler.Apply(item);

        Assert.False(success, $"CleanupOperationHandler should have refused '{forbiddenPath}'");
        Assert.Contains("Safety rejection", message);
        Assert.Equal(0, bytes);
        Assert.Equal(0, files);
    }

    [Fact]
    public void GamingCatalog_AllTweaksMustBeSafeRiskLevel()
    {
        var catalog = TweakEngineService.GetSupportedGamingCatalog();

        Assert.NotEmpty(catalog);
        foreach (var tweak in catalog)
        {
            Assert.Equal(OperationRiskLevel.Safe, tweak.RiskLevel);
            Assert.False(string.IsNullOrWhiteSpace(tweak.Id));
            Assert.False(string.IsNullOrWhiteSpace(tweak.KeyPath));
            Assert.NotNull(tweak.RecommendedValue);
            Assert.NotNull(tweak.DefaultValue);

            // Ensure no dangerous paths
            Assert.DoesNotContain("SAM", tweak.KeyPath, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SECURITY", tweak.KeyPath, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BCD", tweak.KeyPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}

