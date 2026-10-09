namespace WeekPlan;

public enum NutrientRole
{
    Positive,
    Limit,
    Range,
}

public enum NutrientTone
{
    Green,
    Yellow,
    Red,
}

public static class NutrientToneRules
{
    public static NutrientTone Tone(
        NutrientRole role,
        bool energy,
        double current,
        double target,
        bool cutting,
        double? basal)
    {
        if (double.IsNaN(current) || double.IsInfinity(current) || target <= 0 || double.IsNaN(target) || double.IsInfinity(target))
            return NutrientTone.Green;

        var difference = current - target;
        var rate = current / target;
        return role switch
        {
            NutrientRole.Limit => Limit(rate),
            NutrientRole.Range => energy
                ? (cutting ? CutEnergy(current, difference, basal) : Keep(rate))
                : (cutting ? CutMacro(rate) : Keep(rate)),
            _ => Positive(rate),
        };
    }

    private static NutrientTone Positive(double rate)
    {
        if (rate >= 0.9)
            return NutrientTone.Green;
        if (rate >= 0.5)
            return NutrientTone.Yellow;
        return NutrientTone.Red;
    }

    private static NutrientTone Limit(double rate)
    {
        if (rate <= 1)
            return NutrientTone.Green;
        if (rate <= 1.1)
            return NutrientTone.Yellow;
        return NutrientTone.Red;
    }

    private static NutrientTone CutEnergy(double current, double difference, double? basal)
    {
        if (basal is double floor && current < floor)
            return NutrientTone.Red;
        if (difference > 0 || difference < -1000)
            return NutrientTone.Red;
        if (difference >= -500 && difference <= -200)
            return NutrientTone.Green;
        return NutrientTone.Yellow;
    }

    private static NutrientTone CutMacro(double rate)
    {
        if (rate < 0.5)
            return NutrientTone.Yellow;
        if (rate <= 1)
            return NutrientTone.Green;
        if (rate <= 1.1)
            return NutrientTone.Yellow;
        return NutrientTone.Red;
    }

    private static NutrientTone Keep(double rate)
    {
        if (rate < 0.9)
            return NutrientTone.Red;
        if (rate <= 1.1)
            return NutrientTone.Green;
        if (rate <= 1.3)
            return NutrientTone.Yellow;
        return NutrientTone.Red;
    }
}
