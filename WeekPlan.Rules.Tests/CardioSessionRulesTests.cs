using WeekPlan;
using Xunit;

namespace WeekPlan.Rules.Tests;

public class CardioSessionRulesTests
{
    [Fact]
    public void StartSetsTheEndAndASecondStartLeavesIt()
    {
        var fresh = CardioSessionRules.Fresh(30);
        var started = CardioSessionRules.SetStatus(fresh, PlanCodes.InProgress, 1_000);
        Assert.Equal(PlanCodes.InProgress, started.Status);
        Assert.Equal(1_000 + 30 * 60_000L, started.EndsAt);

        var again = CardioSessionRules.SetStatus(started, PlanCodes.InProgress, 9_000);
        Assert.Equal(started, again);
    }

    [Fact]
    public void SyncFinishesOnlyWhenTheClockReachesTheEnd()
    {
        var started = CardioSessionRules.SetStatus(CardioSessionRules.Fresh(1), PlanCodes.InProgress, 5_000);
        var end = 5_000 + 60_000L;
        Assert.Equal(started, CardioSessionRules.Sync(started, end - 1));

        var done = CardioSessionRules.Sync(started, end);
        Assert.Equal(PlanCodes.Done, done.Status);
        Assert.Null(done.EndsAt);
    }

    [Fact]
    public void ResetAndDoneClearTheClock()
    {
        var started = CardioSessionRules.SetStatus(CardioSessionRules.Fresh(20), PlanCodes.InProgress, 2_000);
        var reset = CardioSessionRules.SetStatus(started, PlanCodes.NotStarted, 3_000);
        Assert.Equal(PlanCodes.NotStarted, reset.Status);
        Assert.Null(reset.EndsAt);

        var done = CardioSessionRules.SetStatus(started, PlanCodes.Done, 3_000);
        Assert.Equal(PlanCodes.Done, done.Status);
        Assert.Null(done.EndsAt);
    }
}
