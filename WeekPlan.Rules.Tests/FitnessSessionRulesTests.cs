using WeekPlan;
using Xunit;

namespace WeekPlan.Rules.Tests;

public class FitnessSessionRulesTests
{
    private static SessionSnapshot InProgress(int current, int sets, long? restEnds = null, int rest = 60) =>
        new(PlanCodes.InProgress, current, restEnds, sets, rest);

    [Fact]
    public void CompleteSetStartsRestUntilTheLastSet()
    {
        var next = FitnessSessionRules.CompleteSet(InProgress(1, 3, rest: 45), 1_000);
        Assert.Equal(PlanCodes.InProgress, next.Status);
        Assert.Equal(1, next.CurrentSet);
        Assert.Equal(46_000, next.RestEndsAt);
    }

    [Fact]
    public void LastSetCompletesWithoutRest()
    {
        var next = FitnessSessionRules.CompleteSet(InProgress(3, 3), 1_000);
        Assert.Equal(PlanCodes.Done, next.Status);
        Assert.Null(next.RestEndsAt);
    }

    [Fact]
    public void ClickDuringRestIsIgnored()
    {
        var state = InProgress(1, 3, restEnds: 5_000);
        var next = FitnessSessionRules.CompleteSet(state, 4_000);
        Assert.Equal(state, next);
    }

    [Fact]
    public void ClickWhenRestJustEndedOnlyAdvances()
    {
        var next = FitnessSessionRules.CompleteSet(InProgress(1, 3, restEnds: 5_000), 5_000);
        Assert.Equal(PlanCodes.InProgress, next.Status);
        Assert.Equal(2, next.CurrentSet);
        Assert.Null(next.RestEndsAt);
    }

    [Fact]
    public void SyncRestAdvancesToTheNextSet()
    {
        var next = FitnessSessionRules.SyncRest(InProgress(2, 4, restEnds: 5_000), 5_000);
        Assert.Equal(3, next.CurrentSet);
        Assert.Null(next.RestEndsAt);
    }

    [Fact]
    public void ReducingSetsDuringRestAfterTheNewLastSetMarksDone()
    {
        var next = FitnessSessionRules.ChangeSets(InProgress(3, 4, restEnds: 9_000), 3);
        Assert.Equal(PlanCodes.Done, next.Status);
        Assert.Equal(3, next.Sets);
        Assert.Null(next.RestEndsAt);
    }

    [Fact]
    public void ReducingSetsWhileWorkingClampsTheCurrentSet()
    {
        var next = FitnessSessionRules.ChangeSets(InProgress(4, 4), 2);
        Assert.Equal(PlanCodes.InProgress, next.Status);
        Assert.Equal(2, next.CurrentSet);
        Assert.Equal(2, next.Sets);
    }

    [Fact]
    public void SettingNotStartedClearsProgress()
    {
        var next = FitnessSessionRules.SetStatus(InProgress(2, 4, restEnds: 8_000), PlanCodes.NotStarted);
        Assert.Equal(PlanCodes.NotStarted, next.Status);
        Assert.Equal(1, next.CurrentSet);
        Assert.Null(next.RestEndsAt);
    }

    [Fact]
    public void SettingInProgressKeepsAnExistingSession()
    {
        var state = InProgress(2, 4, restEnds: 8_000);
        Assert.Equal(state, FitnessSessionRules.SetStatus(state, PlanCodes.InProgress));
    }

    [Fact]
    public void SettingInProgressFromDoneRestartsAtSetOne()
    {
        var done = new SessionSnapshot(PlanCodes.Done, 4, null, 4, 60);
        var next = FitnessSessionRules.SetStatus(done, PlanCodes.InProgress);
        Assert.Equal(PlanCodes.InProgress, next.Status);
        Assert.Equal(1, next.CurrentSet);
        Assert.Null(next.RestEndsAt);
    }

    [Fact]
    public void SundayIsTheSeventhPlanDay()
    {
        var thursday = new DateOnly(2026, 10, 8);
        Assert.Equal(4, WeekCalendar.PlanDay(thursday));
        Assert.Equal(new DateOnly(2026, 10, 5), WeekCalendar.DateForPlanDay(1, thursday));
        Assert.Equal(new DateOnly(2026, 10, 11), WeekCalendar.DateForPlanDay(7, thursday));
        Assert.Equal(7, WeekCalendar.PlanDay(new DateOnly(2026, 10, 11)));
    }
}
