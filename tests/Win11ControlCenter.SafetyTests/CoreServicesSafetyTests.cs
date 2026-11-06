using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Win11ControlCenter.SystemEngine.Handlers;
using Xunit;

namespace Win11ControlCenter.SafetyTests;

public class CoreServicesSafetyTests
{
    [Theory]
    [InlineData("RpcSs")]
    [InlineData("WinDefend")]
    [InlineData("EventLog")]
    [InlineData("CryptSvc")]
    [InlineData("Dnscache")]
    [InlineData("Dhcp")]
    [InlineData("MpsSvc")]
    [InlineData("wuauserv")]
    public void CoreServices_AreIdentifiedAsProtected(string serviceName)
    {
        Assert.True(CoreWindowsServices.IsProtectedService(serviceName), $"Service '{serviceName}' must be protected.");
    }

    [Fact]
    public void SafetyValidator_RejectsDisablingCoreService()
    {
        var item = new OperationItem
        {
            Name = "Disable Defender",
            OperationType = OperationType.Service,
            Target = "WinDefend",
            RecommendedValue = "Disabled",
            RiskLevel = OperationRiskLevel.Critical
        };

        var (isValid, reason) = SafetyValidator.ValidateOperation(item);

        Assert.False(isValid);
        Assert.Contains("core Windows infrastructure component", reason);
    }

    [Fact]
    public void ServiceOperationHandler_RefusesToApplyProtectedServiceChange()
    {
        var item = new OperationItem
        {
            Name = "Disable RPC",
            OperationType = OperationType.Service,
            Target = "RpcSs",
            RecommendedValue = "Disabled"
        };

        var (success, message) = ServiceOperationHandler.Apply(item);

        Assert.False(success);
        Assert.Contains("Safety block", message);
    }
}

