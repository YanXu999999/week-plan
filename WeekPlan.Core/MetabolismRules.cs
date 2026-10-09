namespace WeekPlan;

public static class MetabolismRules
{
    public static double? Basal(int? age, double? heightCm, double? weightKg, string? sex)
    {
        if (age is null || heightCm is null || weightKg is null)
            return null;

        var resting = 10d * weightKg.Value + 6.25d * heightCm.Value - 5d * age.Value;
        var calories = BodySex.Normalize(sex) == BodySex.Female ? resting - 161d : resting + 5d;
        return Math.Round(calories, 0, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<string> Missing(int? age, double? heightCm, double? weightKg)
    {
        var missing = new List<string>();
        if (age is null)
            missing.Add("年龄");
        if (heightCm is null)
            missing.Add("身高");
        if (weightKg is null)
            missing.Add("体重");
        return missing;
    }

    public static string Describe(int? age, double? heightCm, double? weightKg, string? sex)
    {
        var missing = Missing(age, heightCm, weightKg);
        if (missing.Count > 0)
            return "缺少" + string.Join("、", missing);

        return NutritionMath.FormatNumber(Basal(age, heightCm, weightKg, sex) ?? 0) + " 千卡";
    }
}
