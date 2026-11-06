using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Xunit;

namespace Win11ControlCenter.SafetyTests;

public class CustomTweaksSafetyTests
{
    [Theory]
    [InlineData("HKLM", @"SAM\SecurityAccounts")]
    [InlineData("HKLM", @"SECURITY\Policies")]
    [InlineData("HKLM", @"BCD00000000\Objects")]
    [InlineData("HKLM", @"SYSTEM\CurrentControlSet\Control\SafeBoot")]
    [InlineData("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\KnownDLLs")]
    public void SafetyValidator_MustBlockDangerousCustomTweaks(string hive, string keyPath)
    {
        var item = new OperationItem
        {
            Name = "Dangerous User Tweak",
            OperationType = OperationType.Registry,
            Target = $"{hive}\\{keyPath}",
            RecommendedValue = 0
        };

        var (isValid, reason) = SafetyValidator.ValidateOperation(item);

        Assert.False(isValid, $"SafetyValidator should have blocked custom tweak targeting: {hive}\\{keyPath}");
        Assert.Contains("protected", reason, StringComparison.OrdinalIgnoreCase);
    }
}
