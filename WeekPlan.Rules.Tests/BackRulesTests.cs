using WeekPlan;
using Xunit;

namespace WeekPlan.Rules.Tests;

public class BackRulesTests
{
    [Fact]
    public void EdgeBackReturnsHomeThenAsksBeforeExit()
    {
        const long now = 1_000_000;
        Assert.Equal(BackStep.ReturnHome, BackRules.Decide(false, now, 0));
        Assert.Equal(BackStep.ConfirmExit, BackRules.Decide(true, now, 0));

        var armed = BackRules.ArmExit(now);
        Assert.Equal(now + 2000, armed);
        Assert.Equal(BackStep.Exit, BackRules.Decide(true, now + 2000, armed));
        Assert.Equal(BackStep.ConfirmExit, BackRules.Decide(true, now + 2001, armed));
        Assert.Equal(BackStep.ReturnHome, BackRules.Decide(false, now + 500, armed));
    }
}
