namespace WeekPlan;

public static class SunExposure
{
    public const string Name = "晒太阳";

    // 夏天全身每分钟约 6.7 微克。露出四分之一皮肤晒 15 分钟，约 25 微克。
    // 微克 = 分钟 × 露肤比例 × 季节系数 × 6.7。
    public const double MicrogramsPerMinuteAtFullSkin = 6.7;

    public static readonly (string Label, double Fraction)[] Levels =
    {
        ("脸和手", 0.10),
        ("加上前臂", 0.25),
        ("短袖短裤", 0.40),
        ("大半身", 0.60),
    };

    public const double Spring = 0.60;
    public const double Summer = 1.00;
    public const double Autumn = 0.50;
    public const double Winter = 0.20;

    public static readonly (string Label, double Factor)[] Seasons =
    {
        ("春", Spring),
        ("夏", Summer),
        ("秋", Autumn),
        ("冬", Winter),
    };

    public static double VitaminDMicrograms(double minutes, double skinFraction, double seasonFactor) =>
        NutritionMath.RoundFine(minutes * skinFraction * seasonFactor * MicrogramsPerMinuteAtFullSkin);

    public static string? Validate(double minutes, double skinFraction, double seasonFactor)
    {
        if (double.IsNaN(minutes) || double.IsInfinity(minutes) || minutes <= 0 || minutes > 720)
            return "晒的时间要大于 0 分钟，并且不超过 720 分钟";
        if (!Levels.Any(level => level.Fraction == skinFraction))
            return "请选择露肤度";
        if (!Seasons.Any(season => season.Factor == seasonFactor))
            return "请选择季节";
        return null;
    }
}
