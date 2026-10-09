namespace WeekPlan;

public sealed record BodyProfile(int? Age, double? HeightCm, string Sex = BodySex.Male, string Goal = NutritionGoal.Keep)
{
    public static BodyProfile Empty { get; } = new(null, null);
}

public sealed record ProfileDraft(int Age, double HeightCm);

public static class ProfileRules
{
    public const int MinAge = 1;
    public const int MaxAge = 120;
    public const double MinHeightCm = 50;
    public const double MaxHeightCm = 250;

    public static double RoundHeight(double heightCm) =>
        Math.Round(heightCm, 1, MidpointRounding.AwayFromZero);

    public static string? ValidateAge(int age)
    {
        if (age < MinAge || age > MaxAge)
            return "年龄要在 1 到 120 岁之间";
        return null;
    }

    public static string? ValidateHeight(double heightCm)
    {
        if (double.IsNaN(heightCm) || double.IsInfinity(heightCm))
            return "身高要填数字";
        if (Math.Abs(heightCm - RoundHeight(heightCm)) > 0.0000001)
            return "身高最多保留一位小数";
        if (heightCm < MinHeightCm || heightCm > MaxHeightCm)
            return "身高要在 50 到 250 厘米之间";
        return null;
    }

    public static string? Validate(ProfileDraft draft) =>
        ValidateAge(draft.Age) ?? ValidateHeight(draft.HeightCm);
}

public static class BodySex
{
    public const string Male = "m";
    public const string Female = "f";

    public static string Normalize(string? sex) => sex == Female ? Female : Male;

    public static string Label(string? sex) => Normalize(sex) == Female ? "女" : "男";
}

public static class NutritionGoal
{
    public const string Cut = "cut";
    public const string Keep = "keep";

    public static string Normalize(string? goal) => goal == Cut ? Cut : Keep;

    public static bool IsCut(string? goal) => Normalize(goal) == Cut;
}
