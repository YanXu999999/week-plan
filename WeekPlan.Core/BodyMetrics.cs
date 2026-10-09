namespace WeekPlan;

public sealed record WeightEntry(long EpochDay, double Kilograms);

public sealed record BodyMeasures(
    long EpochDay,
    double WaistCm,
    double LeftArmCm,
    double RightArmCm,
    double ChestCm,
    double LeftThighCm,
    double RightThighCm);

public sealed record BodyMeasureDraft(
    double WaistCm,
    double LeftArmCm,
    double RightArmCm,
    double ChestCm,
    double LeftThighCm,
    double RightThighCm);

public static class WeightRules
{
    public const double MinKg = 20;
    public const double MaxKg = 300;
    public const int AverageCount = 7;
    public const int KeepDays = 365;

    public static double Round(double kilograms) =>
        Math.Round(kilograms, 1, MidpointRounding.AwayFromZero);

    public static long OldestKeptEpoch(long todayEpoch) => todayEpoch - (KeepDays - 1);

    public static string? Validate(double kilograms)
    {
        if (double.IsNaN(kilograms) || double.IsInfinity(kilograms))
            return "体重要填数字";
        if (Math.Abs(kilograms - Round(kilograms)) > 0.0000001)
            return "体重最多保留一位小数";
        if (kilograms < MinKg || kilograms > MaxKg)
            return "体重要在 20 到 300 千克之间";
        return null;
    }

    public static double? AverageOfLatest(IReadOnlyList<WeightEntry> entries)
    {
        if (entries.Count == 0)
            return null;
        var sample = entries.OrderByDescending(entry => entry.EpochDay).Take(AverageCount).ToList();
        return Round(sample.Average(entry => entry.Kilograms));
    }
}

public static class BodyMeasureRules
{
    public const double MinCm = 10;
    public const double MaxCm = 300;

    public static double Round(double centimeters) =>
        Math.Round(centimeters, 1, MidpointRounding.AwayFromZero);

    public static string? Validate(BodyMeasureDraft draft) =>
        ValidateCentimeter("腰围", draft.WaistCm)
        ?? ValidateCentimeter("左臂围", draft.LeftArmCm)
        ?? ValidateCentimeter("右臂围", draft.RightArmCm)
        ?? ValidateCentimeter("胸围", draft.ChestCm)
        ?? ValidateCentimeter("左腿围", draft.LeftThighCm)
        ?? ValidateCentimeter("右腿围", draft.RightThighCm);

    public static string? ValidateCentimeter(string name, double centimeters)
    {
        if (double.IsNaN(centimeters) || double.IsInfinity(centimeters))
            return $"{name}要填数字";
        if (Math.Abs(centimeters - Round(centimeters)) > 0.0000001)
            return $"{name}最多保留一位小数";
        if (centimeters < MinCm || centimeters > MaxCm)
            return $"{name}要在 10 到 300 厘米之间";
        return null;
    }
}

public static class BodyReminderRules
{
    public static DateOnly MondayOf(DateOnly date)
    {
        var delta = date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1;
        return date.AddDays(-delta);
    }

    public static DateOnly SundayOnOrAfter(DateOnly date)
    {
        var offset = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(offset);
    }

    public static bool ShowEmpty(DateOnly viewedSunday, DateOnly anchorMonday)
    {
        if (viewedSunday.DayOfWeek != DayOfWeek.Sunday)
            return false;
        return MondayOf(viewedSunday) == anchorMonday;
    }

    public static bool IsScheduledSunday(DateOnly sunday, DateOnly lastRecord)
    {
        if (sunday.DayOfWeek != DayOfWeek.Sunday || sunday <= lastRecord)
            return false;
        var origin = SundayOnOrAfter(lastRecord.AddDays(14));
        if (sunday < origin)
            return false;
        return (sunday.DayNumber - origin.DayNumber) % 14 == 0;
    }
}

public readonly record struct ChartAnchor(int Index, double X, double Y);

public static class ChartGeometry
{
    public const double Left = 44;
    public const double Right = 12;
    public const double Top = 16;
    public const double Bottom = 28;

    public static IReadOnlyList<ChartAnchor> Layout(double width, double height, IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return Array.Empty<ChartAnchor>();

        var plotWidth = Math.Max(1, width - Left - Right);
        var plotHeight = Math.Max(1, height - Top - Bottom);
        var min = values.Min();
        var max = values.Max();
        if (Math.Abs(max - min) < 0.001)
        {
            min -= 1;
            max += 1;
        }
        else
        {
            var pad = (max - min) * 0.12;
            min -= pad;
            max += pad;
        }

        var span = max - min;
        var anchors = new ChartAnchor[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            var x = values.Count == 1
                ? Left + plotWidth / 2
                : Left + plotWidth * index / (values.Count - 1);
            var y = Top + plotHeight * (1 - (values[index] - min) / span);
            anchors[index] = new ChartAnchor(index, x, y);
        }

        return anchors;
    }

    public static int HitIndex(IReadOnlyList<ChartAnchor> anchors, double x, double y, double radius)
    {
        var best = -1;
        var bestDistance = radius * radius;
        foreach (var anchor in anchors)
        {
            var dx = anchor.X - x;
            var dy = anchor.Y - y;
            var distance = dx * dx + dy * dy;
            if (distance > bestDistance)
                continue;
            bestDistance = distance;
            best = anchor.Index;
        }

        return best;
    }
}
