namespace WeekPlan;

public sealed record SessionSnapshot(
    string Status,
    int CurrentSet,
    long? RestEndsAt,
    int Sets,
    int RestSeconds);

public static class FitnessSessionRules
{
    public static SessionSnapshot Fresh(int sets, int restSeconds) => new(
        PlanCodes.NotStarted,
        1,
        null,
        Math.Clamp(sets, PlanLimits.MinSets, PlanLimits.MaxSets),
        Math.Clamp(restSeconds, PlanLimits.MinRestSeconds, PlanLimits.MaxRestSeconds));

    public static SessionSnapshot SyncRest(SessionSnapshot state, long now)
    {
        if (state.Status != PlanCodes.InProgress || state.RestEndsAt is not long restEnd || now < restEnd)
            return state;

        var nextSet = state.CurrentSet + 1;
        if (nextSet > state.Sets)
            return state with { Status = PlanCodes.Done, CurrentSet = state.Sets, RestEndsAt = null };

        return state with { CurrentSet = nextSet, RestEndsAt = null };
    }

    public static SessionSnapshot CompleteSet(SessionSnapshot state, long now)
    {
        if (state.Status == PlanCodes.InProgress && state.RestEndsAt is long restEnd)
        {
            if (now < restEnd)
                return state;
            return SyncRest(state, now);
        }

        if (state.Status != PlanCodes.InProgress)
            return state;

        if (state.CurrentSet >= state.Sets)
            return state with { Status = PlanCodes.Done, RestEndsAt = null };

        return state with { RestEndsAt = now + state.RestSeconds * 1000L };
    }

    public static SessionSnapshot SetStatus(SessionSnapshot state, string status)
    {
        return status switch
        {
            PlanCodes.NotStarted => state with
            {
                Status = PlanCodes.NotStarted,
                CurrentSet = 1,
                RestEndsAt = null,
            },
            PlanCodes.Done => state with { Status = PlanCodes.Done, RestEndsAt = null },
            PlanCodes.InProgress when state.Status == PlanCodes.InProgress => state,
            PlanCodes.InProgress => state with
            {
                Status = PlanCodes.InProgress,
                CurrentSet = 1,
                RestEndsAt = null,
            },
            _ => state,
        };
    }

    public static SessionSnapshot ChangeSets(SessionSnapshot state, int newSets)
    {
        var sets = Math.Clamp(newSets, PlanLimits.MinSets, PlanLimits.MaxSets);
        var updated = state with { Sets = sets };
        if (updated.Status == PlanCodes.InProgress && updated.RestEndsAt is not null && updated.CurrentSet >= sets)
            return updated with { Status = PlanCodes.Done, CurrentSet = sets, RestEndsAt = null };

        return updated with { CurrentSet = Math.Clamp(updated.CurrentSet, 1, sets) };
    }
}
