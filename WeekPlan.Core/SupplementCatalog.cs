namespace WeekPlan;

public sealed record SupplementItem(
    string Id,
    string Name,
    string Group,
    string Piece,
    NutrientValues PerServing,
    SupplementDose PerServingDose);

public static class SupplementCatalog
{
    public const string VitaminGroup = "vitamin";
    public const string ProteinGroup = "protein";
    public const string FitnessGroup = "fitness";

    public static IReadOnlyList<SupplementItem> Vitamins { get; } =
    [
        Item("b1", "维生素B1", VitaminGroup, "片", new NutrientValues(0, 0, 0, 0, VitaminB1: 1.2), SupplementDose.Zero),
        Item("c", "维生素C", VitaminGroup, "片", new NutrientValues(0, 0, 0, 0, VitaminC: 100), SupplementDose.Zero),
        Item("d", "维生素D", VitaminGroup, "粒", new NutrientValues(0, 0, 0, 0, VitaminD: 10), SupplementDose.Zero),
        Item("multi", "复合维生素", VitaminGroup, "片", new NutrientValues(0, 0, 0, 0, Calcium: 100, Iron: 5, Zinc: 5, VitaminB1: 1.4, VitaminC: 100, VitaminD: 10), SupplementDose.Zero),
        Item("calcium", "钙片", VitaminGroup, "片", new NutrientValues(0, 0, 0, 0, Calcium: 200), SupplementDose.Zero),
        Item("iron", "铁剂", VitaminGroup, "片", new NutrientValues(0, 0, 0, 0, Iron: 20), SupplementDose.Zero),
        Item("zinc", "锌片", VitaminGroup, "片", new NutrientValues(0, 0, 0, 0, Zinc: 15), SupplementDose.Zero),
        Item("fish-oil", "鱼油", VitaminGroup, "粒", new NutrientValues(0, 0, 0, 0, Omega3: 0.3), SupplementDose.Zero),
    ];

    public static IReadOnlyList<SupplementItem> Proteins { get; } =
    [
        Item("whey", "乳清蛋白", ProteinGroup, "勺", WheyScoop(), SupplementDose.Zero),
    ];

    public static IReadOnlyList<SupplementItem> Fitness { get; } =
    [
        Item("creatine", "肌酸", FitnessGroup, "份", NutrientValues.Zero, new SupplementDose(Creatine: 5)),
        Item("bcaa", "支链氨基酸", FitnessGroup, "份", NutrientValues.Zero, new SupplementDose(Bcaa: 7)),
        Item("glutamine", "谷氨酰胺", FitnessGroup, "份", NutrientValues.Zero, new SupplementDose(Glutamine: 5)),
        Item("carnitine", "左旋肉碱", FitnessGroup, "粒", NutrientValues.Zero, new SupplementDose(Carnitine: 500)),
        Item("electrolyte", "电解质", FitnessGroup, "份", NutrientValues.Zero, new SupplementDose(Electrolyte: 1)),
    ];

    public static SupplementItem? Find(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        foreach (var item in Vitamins)
        {
            if (item.Id == id)
                return item;
        }

        foreach (var item in Proteins)
        {
            if (item.Id == id)
                return item;
        }

        foreach (var item in Fitness)
        {
            if (item.Id == id)
                return item;
        }

        return null;
    }

    private static NutrientValues WheyScoop()
    {
        var powder = NutritionCatalog.BuiltIn.Single(food => food.Name == "蛋白粉");
        return powder.PerHundred.Scale(30);
    }

    private static SupplementItem Item(
        string id,
        string name,
        string group,
        string piece,
        NutrientValues perServing,
        SupplementDose perServingDose) =>
        new(id, name, group, piece, perServing, perServingDose);
}
