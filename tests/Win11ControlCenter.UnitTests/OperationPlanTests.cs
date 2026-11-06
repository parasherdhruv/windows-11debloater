using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Models;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class OperationPlanTests
{
    [Fact]
    public void OperationPlan_CalculatesCountsAndHighestRiskCorrectly()
    {
        var plan = new OperationPlan
        {
            Title = "Test Plan",
            Items = new List<OperationItem>
            {
                new() { Name = "Safe Item 1", RiskLevel = OperationRiskLevel.Safe, Status = ExecutionStatus.Succeeded },
                new() { Name = "Safe Item 2", RiskLevel = OperationRiskLevel.Safe, Status = ExecutionStatus.Succeeded },
                new() { Name = "Advanced Item", RiskLevel = OperationRiskLevel.Advanced, Status = ExecutionStatus.Pending }
            }
        };

        Assert.Equal(3, plan.TotalItems);
        Assert.Equal(2, plan.SucceededCount);
        Assert.Equal(0, plan.FailedCount);
        Assert.Equal(OperationRiskLevel.Advanced, plan.HighestRiskLevel);
        Assert.False(plan.OverallSuccess); // Pending items remain
    }

    [Fact]
    public void OperationPlan_OverallSuccessIsTrue_WhenAllItemsSucceedOrSkipped()
    {
        var plan = new OperationPlan
        {
            Title = "Completed Plan",
            Items = new List<OperationItem>
            {
                new() { Name = "Item 1", Status = ExecutionStatus.Succeeded },
                new() { Name = "Item 2", Status = ExecutionStatus.Skipped }
            }
        };

        Assert.True(plan.OverallSuccess);
    }
}

