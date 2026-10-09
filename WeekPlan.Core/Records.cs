namespace WeekPlan;

public sealed record TaskDraft(
    long? TaskId,
    int DayOfWeek,
    long EpochDay,
    string Type,
    string Title,
    int Sets,
    int RestSeconds,
    string? Status,
    int DurationMinutes = PlanLimits.DefaultDurationMinutes);

public sealed record TaskItem(
    long Id,
    int DayOfWeek,
    string Type,
    string Title,
    int Sets,
    int RestSeconds,
    string Status,
    int CurrentSet,
    long? RestEndsAt,
    int DurationMinutes);

public sealed record SessionItem(
    long Id,
    string Title,
    int Sets,
    int RestSeconds,
    string Status,
    int CurrentSet,
    long? RestEndsAt,
    string Type = PlanCodes.Strength,
    int DurationMinutes = PlanLimits.DefaultDurationMinutes);

public sealed record EditorItem(
    long? TaskId,
    int DayOfWeek,
    long EpochDay,
    bool Creating,
    string Title,
    string Type,
    int Sets,
    int RestSeconds,
    string Status,
    int DurationMinutes = PlanLimits.DefaultDurationMinutes);
