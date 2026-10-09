namespace WeekPlan;

public enum BackStep
{
    ReturnHome,
    ConfirmExit,
    Exit,
}

public static class BackRules
{
    public const long ExitConfirmMillis = 2000;

    public static BackStep Decide(bool atHome, long nowMillis, long armedUntil)
    {
        if (!atHome)
            return BackStep.ReturnHome;
        if (armedUntil > 0 && nowMillis <= armedUntil)
            return BackStep.Exit;
        return BackStep.ConfirmExit;
    }

    public static long ArmExit(long nowMillis) => nowMillis + ExitConfirmMillis;
}
