namespace WeekPlan;

public static class NutritionMath
{
    public static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";
        return string.Join(" ", name.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
    }

    public const int ApproximateRun = 2;

    public const int ApproximateLimit = 8;

    public static string NameKey(string? name) => NormalizeName(name).ToLowerInvariant();

    public static int SharedRunLength(string? left, string? right)
    {
        var a = NameKey(left);
        var b = NameKey(right);
        if (a.Length == 0 || b.Length == 0)
            return 0;

        var best = 0;
        var previous = new int[b.Length + 1];
        for (var i = 0; i < a.Length; i++)
        {
            var current = new int[b.Length + 1];
            for (var j = 0; j < b.Length; j++)
            {
                if (a[i] != b[j])
                    continue;
                current[j + 1] = previous[j] + 1;
                if (current[j + 1] > best)
                    best = current[j + 1];
            }

            previous = current;
        }

        return best;
    }

    public static bool IsApproximateName(string? left, string? right)
    {
        var a = NameKey(left);
        var b = NameKey(right);
        if (a.Length == 0 || b.Length == 0 || a == b)
            return false;
        return SharedRunLength(a, b) >= ApproximateRun;
    }

    public static double Round(double value) =>
        Math.Round(value, 1, MidpointRounding.AwayFromZero);

    public static double RoundFine(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static double? PercentOfTarget(double current, double target)
    {
        if (target <= 0 || double.IsNaN(target) || double.IsInfinity(target) || double.IsNaN(current) || double.IsInfinity(current))
            return null;
        return Round(current / target * 100d);
    }

    public static string FormatNumber(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "0";
        var rounded = RoundFine(value);
        var whole = Math.Round(rounded, 0, MidpointRounding.AwayFromZero);
        if (Math.Abs(rounded - whole) < 0.001)
            return whole.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        var tenth = Math.Round(rounded, 1, MidpointRounding.AwayFromZero);
        if (Math.Abs(rounded - tenth) < 0.001)
            return tenth.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        return rounded.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    public static string FormatPair(double current, double target) =>
        $"{FormatNumber(current)} / {FormatNumber(target)}";

    public static string FormatSigned(double current, double target)
    {
        var diff = RoundFine(current - target);
        var text = FormatNumber(diff);
        return diff > 0 ? "+" + text : text;
    }

    public static string FormatPercent(double current, double target)
    {
        var percent = PercentOfTarget(current, target);
        return percent is null ? "—" : FormatNumber(percent.Value) + "%";
    }

    public static string UnitLabel(string unit) => unit switch
    {
        NutritionUnits.Milliliter => "毫升",
        NutritionUnits.Minute => "分钟",
        NutritionUnits.Serving => "份",
        _ => "克",
    };

    public static string PerHundredLabel(string unit) =>
        unit == NutritionUnits.Milliliter ? "每 100 毫升" : "每 100 克";

    public static string? ValidatePerHundred(NutrientValues values)
    {
        if (Bad(values.Calories) || Bad(values.Protein) || Bad(values.Fat) || Bad(values.Carbs)
            || Bad(values.Fiber) || Bad(values.Sodium) || Bad(values.Calcium) || Bad(values.Iron)
            || Bad(values.Zinc) || Bad(values.Potassium) || Bad(values.VitaminB1) || Bad(values.VitaminC)
            || Bad(values.VitaminD) || Bad(values.Omega3))
            return "每 100 单位的营养不能小于 0";
        if (values.Calories > 900)
            return "每 100 单位的热量不能超过 900 千卡";
        if (values.Protein > 100 || values.Fat > 100 || values.Carbs > 100 || values.Fiber > 100 || values.Omega3 > 100)
            return "每 100 单位的蛋白质、脂肪、碳水、膳食纤维和 Omega-3 不能超过 100 克";
        if (values.Sodium > 40000)
            return "每 100 单位的钠不能超过 40000 毫克";
        if (values.Calcium > 5000 || values.Potassium > 5000)
            return "每 100 单位的钙和钾不能超过 5000 毫克";
        if (values.Iron > 100 || values.Zinc > 100)
            return "每 100 单位的铁和锌不能超过 100 毫克";
        if (values.VitaminB1 > 50)
            return "每 100 单位的维生素B1不能超过 50 毫克";
        if (values.VitaminC > 2000)
            return "每 100 单位的维生素C不能超过 2000 毫克";
        if (values.VitaminD > 250)
            return "每 100 单位的维生素D不能超过 250 微克";
        return null;
    }

    public static string? ValidateTargets(TargetDraft draft)
    {
        if (Bad(draft.Calories) || Bad(draft.Protein) || Bad(draft.Fat) || Bad(draft.Carbs)
            || Bad(draft.Fiber) || Bad(draft.Sodium) || Bad(draft.Calcium) || Bad(draft.Iron)
            || Bad(draft.Zinc) || Bad(draft.Potassium) || Bad(draft.VitaminB1) || Bad(draft.VitaminC)
            || Bad(draft.VitaminD) || Bad(draft.Omega3))
            return "目标不能小于 0";
        if (draft.Calories > 20000 || draft.Protein > 2000 || draft.Fat > 2000 || draft.Carbs > 2000
            || draft.Fiber > 200 || draft.Sodium > 20000 || draft.Calcium > 10000 || draft.Iron > 200
            || draft.Zinc > 200 || draft.Potassium > 20000 || draft.VitaminB1 > 100 || draft.VitaminC > 5000
            || draft.VitaminD > 250 || draft.Omega3 > 100)
            return "目标太大了";
        return null;
    }

    private static bool Bad(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) || value < 0;
}
