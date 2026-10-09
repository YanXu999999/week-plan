using WeekPlan;
using Xunit;

namespace WeekPlan.Rules.Tests;

public class NutritionPortionTests
{
    [Fact]
    public void EveryCatalogFoodHasHouseholdPortion()
    {
        var corn = NutritionCatalog.Find("玉米");
        Assert.NotNull(corn);
        Assert.Equal("一根", corn.Portion.Label);
        Assert.Equal(200d, corn.Portion.Amount);
        Assert.Equal("一根约 200 克", corn.Portion.Describe(corn.Unit));
        Assert.Equal("一把", NutritionCatalog.Find("玉米面")!.Portion.Label);
        Assert.Equal("一个", NutritionCatalog.Find("鸡蛋")!.Portion.Label);
        Assert.Equal(50d, NutritionCatalog.Find("鸡蛋")!.Portion.Amount);
        Assert.Equal("一碗", NutritionCatalog.Find("米饭")!.Portion.Label);
        Assert.Equal(200d, NutritionCatalog.Find("米饭")!.Portion.Amount);
        Assert.Equal("一杯", NutritionCatalog.Find("牛奶")!.Portion.Label);
        Assert.Equal(250d, NutritionCatalog.Find("牛奶")!.Portion.Amount);
        Assert.Equal(NutritionUnits.Milliliter, NutritionCatalog.Find("牛奶")!.Unit);
        Assert.Equal("一勺", NutritionCatalog.Find("橄榄油")!.Portion.Label);
        Assert.Equal(10d, NutritionCatalog.Find("橄榄油")!.Portion.Amount);
        Assert.Equal("一个", NutritionCatalog.Find("青椒")!.Portion.Label);
        Assert.Equal("一个", NutritionCatalog.Find("粽子")!.Portion.Label);
        Assert.Equal(200d, NutritionCatalog.Find("粽子")!.Portion.Amount);
        Assert.Equal("一勺", NutritionCatalog.Find("郫县豆瓣")!.Portion.Label);
        Assert.Equal("一碗", NutritionCatalog.Find("面片")!.Portion.Label);
        Assert.Equal(corn, NutritionCatalog.Find("  玉米  "));
        Assert.Null(NutritionCatalog.Find("  "));
        Assert.Null(NutritionCatalog.Find("没有这种食物"));

        foreach (var food in NutritionCatalog.BuiltIn)
        {
            Assert.False(string.IsNullOrWhiteSpace(food.Portion.Label));
            Assert.InRange(food.Portion.Amount, 1d, 1500d);
        }
    }
}
