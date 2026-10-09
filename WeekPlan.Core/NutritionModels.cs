namespace WeekPlan;

public static class NutritionUnits
{
    public const string Gram = "g";
    public const string Milliliter = "ml";
    public const string Minute = "min";
    public const string Serving = "srv";

    public static bool IsKnown(string unit) => unit == Gram || unit == Milliliter;
}

public readonly record struct NutrientValues(
    double Calories,
    double Protein,
    double Fat,
    double Carbs,
    double Fiber = 0,
    double Sodium = 0,
    double Calcium = 0,
    double Iron = 0,
    double Zinc = 0,
    double Potassium = 0,
    double VitaminB1 = 0,
    double VitaminC = 0,
    double VitaminD = 0,
    double Omega3 = 0)
{
    public static NutrientValues Zero => new(0, 0, 0, 0);

    public NutrientValues Add(NutrientValues other) => new(
        Calories + other.Calories,
        Protein + other.Protein,
        Fat + other.Fat,
        Carbs + other.Carbs,
        Fiber + other.Fiber,
        Sodium + other.Sodium,
        Calcium + other.Calcium,
        Iron + other.Iron,
        Zinc + other.Zinc,
        Potassium + other.Potassium,
        VitaminB1 + other.VitaminB1,
        VitaminC + other.VitaminC,
        VitaminD + other.VitaminD,
        Omega3 + other.Omega3);

    public NutrientValues Scale(double amount)
    {
        var factor = amount / 100d;
        return new(
            NutritionMath.Round(Calories * factor),
            NutritionMath.Round(Protein * factor),
            NutritionMath.Round(Fat * factor),
            NutritionMath.Round(Carbs * factor),
            NutritionMath.RoundFine(Fiber * factor),
            NutritionMath.RoundFine(Sodium * factor),
            NutritionMath.RoundFine(Calcium * factor),
            NutritionMath.RoundFine(Iron * factor),
            NutritionMath.RoundFine(Zinc * factor),
            NutritionMath.RoundFine(Potassium * factor),
            NutritionMath.RoundFine(VitaminB1 * factor),
            NutritionMath.RoundFine(VitaminC * factor),
            NutritionMath.RoundFine(VitaminD * factor),
            NutritionMath.RoundFine(Omega3 * factor));
    }

    public NutrientValues Multiply(double factor) => new(
        NutritionMath.Round(Calories * factor),
        NutritionMath.Round(Protein * factor),
        NutritionMath.Round(Fat * factor),
        NutritionMath.Round(Carbs * factor),
        NutritionMath.RoundFine(Fiber * factor),
        NutritionMath.RoundFine(Sodium * factor),
        NutritionMath.RoundFine(Calcium * factor),
        NutritionMath.RoundFine(Iron * factor),
        NutritionMath.RoundFine(Zinc * factor),
        NutritionMath.RoundFine(Potassium * factor),
        NutritionMath.RoundFine(VitaminB1 * factor),
        NutritionMath.RoundFine(VitaminC * factor),
        NutritionMath.RoundFine(VitaminD * factor),
        NutritionMath.RoundFine(Omega3 * factor));
}

public sealed record NutritionTargets(
    double Calories,
    double Protein,
    double Fat,
    double Carbs,
    double Fiber = 25,
    double Sodium = 1500,
    double Calcium = 800,
    double Iron = 12,
    double Zinc = 12,
    double Potassium = 2000,
    double VitaminB1 = 1.4,
    double VitaminC = 100,
    double VitaminD = 10,
    double Omega3 = 1.4)
{
    public const double DefaultCalories = 2050;
    public const double DefaultProtein = 120;
    public const double DefaultFat = 57;
    public const double DefaultCarbs = 250;
    public const double DefaultFiber = 25;
    public const double DefaultSodium = 1500;
    public const double DefaultCalcium = 800;
    public const double DefaultIron = 12;
    public const double DefaultZinc = 12;
    public const double DefaultPotassium = 2000;
    public const double DefaultVitaminB1 = 1.4;
    public const double DefaultVitaminC = 100;
    public const double DefaultVitaminD = 10;
    public const double DefaultOmega3 = 1.4;

    public static NutritionTargets Default { get; } = new(
        DefaultCalories,
        DefaultProtein,
        DefaultFat,
        DefaultCarbs);
}

public sealed class NutritionFact
{
    public NutritionFact(string name, string unit, bool onHome, NutrientRole role, Func<NutrientValues, double> value, Func<NutritionTargets, double> target)
    {
        Name = name;
        Unit = unit;
        OnHome = onHome;
        Role = role;
        Value = value;
        Target = target;
    }

    public string Name { get; }
    public string Unit { get; }
    public bool OnHome { get; }
    public NutrientRole Role { get; }
    public bool Energy => Name == "热量";
    public Func<NutrientValues, double> Value { get; }
    public Func<NutritionTargets, double> Target { get; }

    public static IReadOnlyList<NutritionFact> All { get; } = new NutritionFact[]
    {
        new("热量", "千卡", true, NutrientRole.Range, item => item.Calories, item => item.Calories),
        new("蛋白质", "克", true, NutrientRole.Positive, item => item.Protein, item => item.Protein),
        new("脂肪", "克", true, NutrientRole.Range, item => item.Fat, item => item.Fat),
        new("碳水", "克", true, NutrientRole.Range, item => item.Carbs, item => item.Carbs),
        new("膳食纤维", "克", true, NutrientRole.Positive, item => item.Fiber, item => item.Fiber),
        new("钠", "毫克", true, NutrientRole.Limit, item => item.Sodium, item => item.Sodium),
        new("钙", "毫克", false, NutrientRole.Positive, item => item.Calcium, item => item.Calcium),
        new("铁", "毫克", false, NutrientRole.Positive, item => item.Iron, item => item.Iron),
        new("锌", "毫克", false, NutrientRole.Positive, item => item.Zinc, item => item.Zinc),
        new("钾", "毫克", false, NutrientRole.Positive, item => item.Potassium, item => item.Potassium),
        new("维生素B1", "毫克", false, NutrientRole.Positive, item => item.VitaminB1, item => item.VitaminB1),
        new("维生素C", "毫克", false, NutrientRole.Positive, item => item.VitaminC, item => item.VitaminC),
        new("维生素D", "微克", false, NutrientRole.Positive, item => item.VitaminD, item => item.VitaminD),
        new("Omega-3", "克", false, NutrientRole.Positive, item => item.Omega3, item => item.Omega3),
    };
}

public readonly record struct SupplementDose(
    double Creatine = 0,
    double Bcaa = 0,
    double Glutamine = 0,
    double Carnitine = 0,
    double Electrolyte = 0)
{
    public static SupplementDose Zero => new();

    public SupplementDose Add(SupplementDose other) => new(
        Creatine + other.Creatine,
        Bcaa + other.Bcaa,
        Glutamine + other.Glutamine,
        Carnitine + other.Carnitine,
        Electrolyte + other.Electrolyte);

    public SupplementDose Multiply(double factor) => new(
        NutritionMath.RoundFine(Creatine * factor),
        NutritionMath.RoundFine(Bcaa * factor),
        NutritionMath.RoundFine(Glutamine * factor),
        NutritionMath.RoundFine(Carnitine * factor),
        NutritionMath.RoundFine(Electrolyte * factor));
}

public sealed class SupplementFact
{
    public SupplementFact(string name, string unit, Func<SupplementDose, double> value)
    {
        Name = name;
        Unit = unit;
        Value = value;
    }

    public string Name { get; }
    public string Unit { get; }
    public Func<SupplementDose, double> Value { get; }

    public static IReadOnlyList<SupplementFact> All { get; } = new SupplementFact[]
    {
        new("肌酸", "克", item => item.Creatine),
        new("支链氨基酸", "克", item => item.Bcaa),
        new("谷氨酰胺", "克", item => item.Glutamine),
        new("左旋肉碱", "毫克", item => item.Carnitine),
        new("电解质", "份", item => item.Electrolyte),
    };
}

public sealed record IntakeEntry(
    long Id,
    long EpochDay,
    string FoodName,
    double Amount,
    string Unit,
    NutrientValues Nutrients,
    SupplementDose Supplements);

public sealed record DayNutrition(
    long EpochDay,
    NutritionTargets Targets,
    NutrientValues Current,
    SupplementDose Supplements,
    IReadOnlyList<IntakeEntry> Entries)
{
    public static DayNutrition Empty(long epochDay) =>
        new(epochDay, NutritionTargets.Default, NutrientValues.Zero, SupplementDose.Zero, Array.Empty<IntakeEntry>());
}

public sealed record IntakeDraft(
    long EpochDay,
    string Name,
    double Amount,
    string Unit,
    NutrientValues? PerHundred);

public sealed record FoodCandidate(string Name, string Unit);

public enum IntakeSaveKind
{
    Saved,
    NeedDefinition,
    NeedChoice,
    Invalid,
}

public sealed record IntakeSaveResult(IntakeSaveKind Kind, string Message, IReadOnlyList<FoodCandidate> Similar)
{
    public static IntakeSaveResult Saved { get; } = new(IntakeSaveKind.Saved, "", Array.Empty<FoodCandidate>());

    public static IntakeSaveResult NeedDefinition(string message) =>
        new(IntakeSaveKind.NeedDefinition, message, Array.Empty<FoodCandidate>());

    public static IntakeSaveResult NeedChoice(string message, IReadOnlyList<FoodCandidate> similar) =>
        new(IntakeSaveKind.NeedChoice, message, similar);

    public static IntakeSaveResult Invalid(string message) =>
        new(IntakeSaveKind.Invalid, message, Array.Empty<FoodCandidate>());
}

public sealed record TargetDraft(
    double Calories,
    double Protein,
    double Fat,
    double Carbs,
    double Fiber = NutritionTargets.DefaultFiber,
    double Sodium = NutritionTargets.DefaultSodium,
    double Calcium = NutritionTargets.DefaultCalcium,
    double Iron = NutritionTargets.DefaultIron,
    double Zinc = NutritionTargets.DefaultZinc,
    double Potassium = NutritionTargets.DefaultPotassium,
    double VitaminB1 = NutritionTargets.DefaultVitaminB1,
    double VitaminC = NutritionTargets.DefaultVitaminC,
    double VitaminD = NutritionTargets.DefaultVitaminD,
    double Omega3 = NutritionTargets.DefaultOmega3);
