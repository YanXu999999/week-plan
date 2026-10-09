namespace WeekPlan;

public sealed record CardioSnapshot(
    string Status,
    long? EndsAt,
    int DurationMinutes);

public static class CardioSessionRules
{
    public static int Clamp(int minutes) =>
        Math.Clamp(minutes, PlanLimits.MinDurationMinutes, PlanLimits.MaxDurationMinutes);

    public static CardioSnapshot Fresh(int minutes) => new(
        PlanCodes.NotStarted,
        null,
        Clamp(minutes));

    public static CardioSnapshot Sync(CardioSnapshot state, long now)
    {
        if (state.Status != PlanCodes.InProgress || state.EndsAt is not long end || now < end)
            return state;

        return state with { Status = PlanCodes.Done, EndsAt = null };
    }

    public static CardioSnapshot SetStatus(CardioSnapshot state, string status, long now)
    {
        return status switch
        {
            PlanCodes.NotStarted => state with
            {
                Status = PlanCodes.NotStarted,
                EndsAt = null,
            },
            PlanCodes.Done => state with { Status = PlanCodes.Done, EndsAt = null },
            PlanCodes.InProgress when state.Status == PlanCodes.InProgress => state,
            PlanCodes.InProgress => state with
            {
                Status = PlanCodes.InProgress,
                EndsAt = now + state.DurationMinutes * 60_000L,
            },
            _ => state,
        };
    }
}
