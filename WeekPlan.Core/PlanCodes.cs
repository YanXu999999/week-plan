namespace WeekPlan;

public static class PlanCodes
{
    public const string Fitness = "FITNESS";
    public const string Strength = "STRENGTH";
    public const string Cardio = "CARDIO";
    public const string NotStarted = "NOT_STARTED";
    public const string InProgress = "IN_PROGRESS";
    public const string Done = "DONE";

    public static bool IsCardio(string type) => type == Cardio;

    public static bool IsStrength(string type) => type == Strength || type == Fitness;

    public static bool IsTraining(string type) => IsStrength(type) || IsCardio(type);

    public static string Canonical(string type) => type == Cardio ? Cardio : Strength;
}

public static class PlanLimits
{
    public const int MinSets = 1;
    public const int MaxSets = 20;
    public const int DefaultSets = 3;
    public const int MinRestSeconds = 5;
    public const int MaxRestSeconds = 600;
    public const int DefaultRestSeconds = 60;
    public const int RestStepSeconds = 5;
    public const int MinDurationMinutes = 1;
    public const int MaxDurationMinutes = 180;
    public const int DefaultDurationMinutes = 30;
    public const int DurationStepMinutes = 1;
}
