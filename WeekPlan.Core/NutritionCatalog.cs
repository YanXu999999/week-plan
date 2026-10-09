namespace WeekPlan;

public sealed record CatalogFood(string Name, string Unit, NutrientValues PerHundred, FoodPortion Portion);

public static partial class NutritionCatalog
{
    // 食谱内容变化时加一，启动时用来跳过重复写入。
    public const int Revision = 2;

    // 每 100 克或每 100 毫升的参考成分。
    public static IReadOnlyList<CatalogFood> BuiltIn { get; } = CreateBuiltIn();

    private static readonly Dictionary<string, CatalogFood> ByName =
        BuiltIn.ToDictionary(food => NutritionMath.NameKey(food.Name));

    public static CatalogFood? Find(string? name)
    {
        var key = NutritionMath.NameKey(name);
        if (key.Length == 0)
            return null;
        return ByName.TryGetValue(key, out var food) ? food : null;
    }

    private static CatalogFood[] CreateBuiltIn()
    {
        var all = new List<CatalogFood>
        {
        Item("米饭", NutritionUnits.Gram, 116, 2.6, 0.3, 25.9, 0.3, 2.5, 7, 0.2, 0.5, 30, 0.02, 0, 0, 0.01),
        Item("糙米饭", NutritionUnits.Gram, 111, 2.6, 0.9, 23.0, 1.8, 4, 10, 0.5, 0.8, 86, 0.11, 0, 0, 0.03),
        Item("燕麦片", NutritionUnits.Gram, 377, 15.0, 6.7, 66.9, 10.6, 4, 54, 4.7, 3.0, 429, 0.49, 0, 0, 0.11),
        Item("面条", NutritionUnits.Gram, 109, 4.2, 0.7, 22.8, 1.2, 3, 15, 1.0, 0.6, 40, 0.10, 0, 0, 0.01),
        Item("全麦面包", NutritionUnits.Gram, 246, 8.5, 3.4, 45.1, 6.8, 450, 100, 2.5, 1.8, 250, 0.40, 0, 0, 0.05),
        Item("红薯", NutritionUnits.Gram, 102, 1.6, 0.2, 24.7, 3.0, 28, 24, 0.5, 0.2, 337, 0.06, 26, 0, 0.01),
        Item("土豆", NutritionUnits.Gram, 77, 2.0, 0.2, 17.2, 1.8, 6, 8, 0.8, 0.3, 342, 0.08, 27, 0, 0.01),
        Item("鸡胸肉", NutritionUnits.Gram, 133, 24.6, 1.9, 0, 0, 65, 9, 0.6, 0.7, 338, 0.07, 0, 0.1, 0.03),
        Item("鸡腿肉", NutritionUnits.Gram, 181, 16.0, 13.0, 0, 0, 85, 10, 0.9, 1.5, 260, 0.08, 0, 0.1, 0.08),
        Item("鸡蛋", NutritionUnits.Gram, 144, 13.3, 8.8, 2.8, 0, 131, 56, 2.0, 1.1, 154, 0.11, 0, 2.0, 0.07),
        Item("鸡蛋白", NutritionUnits.Gram, 52, 11.1, 0.2, 0.7, 0, 166, 9, 0.1, 0.03, 163, 0.01, 0, 0, 0),
        Item("牛肉", NutritionUnits.Gram, 125, 20.1, 4.2, 0, 0, 53, 9, 2.6, 4.1, 284, 0.07, 0, 0.1, 0.05),
        Item("猪里脊", NutritionUnits.Gram, 155, 20.3, 7.9, 0.7, 0, 45, 6, 1.0, 1.9, 317, 0.47, 0, 0.2, 0.03),
        Item("三文鱼", NutritionUnits.Gram, 139, 17.2, 7.8, 0, 0, 59, 9, 0.3, 0.4, 363, 0.23, 0, 11, 2.2),
        Item("虾仁", NutritionUnits.Gram, 87, 18.6, 0.8, 0.9, 0, 165, 62, 1.5, 1.3, 185, 0.02, 0, 0.1, 0.3),
        Item("鳕鱼", NutritionUnits.Gram, 88, 20.4, 0.5, 0, 0, 74, 28, 0.4, 0.4, 244, 0.08, 1, 0.8, 0.2),
        Item("豆腐", NutritionUnits.Gram, 81, 8.1, 3.7, 3.8, 0.4, 7, 164, 1.9, 0.6, 125, 0.05, 0, 0, 0.3),
        Item("牛奶", NutritionUnits.Milliliter, 54, 3.0, 3.2, 3.4, 0, 37, 104, 0.1, 0.4, 109, 0.03, 1, 0.1, 0.02),
        Item("酸奶", NutritionUnits.Gram, 72, 2.5, 2.7, 9.3, 0, 40, 118, 0.1, 0.5, 150, 0.03, 1, 0.1, 0.02),
        Item("希腊酸奶", NutritionUnits.Gram, 97, 9.0, 5.0, 3.9, 0, 36, 115, 0.1, 0.5, 141, 0.03, 0, 0.1, 0.02),
        Item("豆浆", NutritionUnits.Milliliter, 31, 1.8, 0.7, 1.1, 1.1, 3, 10, 0.5, 0.2, 48, 0.02, 0, 0, 0.2),
        Item("蛋白粉", NutritionUnits.Gram, 400, 80, 5, 8, 0, 200, 500, 5, 2, 600, 0.5, 0, 0, 0.1),
        Item("西兰花", NutritionUnits.Gram, 34, 2.8, 0.4, 6.6, 2.6, 33, 47, 0.7, 0.4, 316, 0.07, 51, 0, 0.1),
        Item("菠菜", NutritionUnits.Gram, 28, 2.6, 0.3, 4.5, 1.7, 85, 66, 2.9, 0.5, 311, 0.04, 32, 0, 0.14),
        Item("番茄", NutritionUnits.Gram, 19, 0.9, 0.2, 4.0, 1.2, 5, 10, 0.4, 0.1, 163, 0.03, 19, 0, 0),
        Item("黄瓜", NutritionUnits.Gram, 16, 0.8, 0.2, 2.9, 0.5, 2, 16, 0.3, 0.2, 102, 0.02, 9, 0, 0),
        Item("香蕉", NutritionUnits.Gram, 93, 1.4, 0.2, 20.8, 1.2, 1, 7, 0.3, 0.2, 256, 0.02, 8, 0, 0.03),
        Item("苹果", NutritionUnits.Gram, 53, 0.2, 0.2, 13.7, 2.4, 1, 4, 0.1, 0.1, 119, 0.06, 4, 0, 0.01),
        Item("花生", NutritionUnits.Gram, 574, 24.8, 44.3, 21.7, 7.7, 3, 67, 1.7, 2.5, 587, 0.48, 2, 0, 0.04),
        Item("杏仁", NutritionUnits.Gram, 578, 21.2, 49.4, 21.6, 11.8, 1, 264, 3.7, 3.1, 705, 0.21, 0, 0, 0.01),
        Item("橄榄油", NutritionUnits.Milliliter, 899, 0, 99.9, 0, 0, 2, 1, 0.6, 0, 1, 0, 0, 0, 0.76),
        Item("橙汁", NutritionUnits.Milliliter, 45, 0.7, 0.2, 10.4, 0.2, 1, 11, 0.2, 0.1, 200, 0.09, 50, 0, 0),
        Item("黑咖啡", NutritionUnits.Milliliter, 1, 0.1, 0, 0, 0, 2, 2, 0, 0, 49, 0, 0, 0, 0),
            Item("水", NutritionUnits.Milliliter, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        };
        AddChina(all);
        return all.ToArray();
    }

    private static CatalogFood Item(
        string name,
        string unit,
        double calories,
        double protein,
        double fat,
        double carbs,
        double fiber,
        double sodium,
        double calcium,
        double iron,
        double zinc,
        double potassium,
        double vitaminB1,
        double vitaminC,
        double vitaminD,
        double omega3) =>
        new(name, unit, new NutrientValues(
            calories,
            protein,
            fat,
            carbs,
            fiber,
            sodium,
            calcium,
            iron,
            zinc,
            potassium,
            vitaminB1,
            vitaminC,
            vitaminD,
            omega3), NutritionPortions.For(name, unit));
}
