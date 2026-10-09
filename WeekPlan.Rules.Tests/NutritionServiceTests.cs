using WeekPlan;
using Xunit;

namespace WeekPlan.Rules.Tests;

public class NutritionServiceTests
{
    [Fact]
    public void ScalesChickenBreastAndPercentAgainstTarget()
    {
        var perHundred = new NutrientValues(133, 24.6, 1.9, 0);
        var scaled = perHundred.Scale(200);
        Assert.Equal(266d, scaled.Calories);
        Assert.Equal(49.2, scaled.Protein);
        Assert.Equal(3.8, scaled.Fat);
        Assert.Equal(0d, scaled.Carbs);
        Assert.Equal("13.3%", NutritionMath.FormatPercent(scaled.Calories, 2000));
        Assert.Equal("32.8%", NutritionMath.FormatPercent(scaled.Protein, 150));
        Assert.Equal("-1734", NutritionMath.FormatSigned(scaled.Calories, 2000));
        Assert.Null(NutritionMath.PercentOfTarget(10, 0));
        Assert.Equal("—", NutritionMath.FormatPercent(10, 0));
    }

    [Fact]
    public async Task RecordsKnownFoodCustomFoodAndKeepsDaysApart()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-nutrition-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new NutritionService(database);
        var today = WeekCalendar.DayKey(new DateOnly(2026, 10, 8));
        var tomorrow = WeekCalendar.DayKey(new DateOnly(2026, 10, 9));

        var defaults = await service.GetTargetsAsync();
        Assert.Equal(2050d, defaults.Calories);
        Assert.Equal(120d, defaults.Protein);
        Assert.Equal(57d, defaults.Fat);
        Assert.Equal(250d, defaults.Carbs);
        Assert.Equal(25d, defaults.Fiber);
        Assert.Equal(1500d, defaults.Sodium);
        Assert.Equal(12d, defaults.Iron);
        Assert.Equal(12d, defaults.Zinc);
        Assert.Equal(1.4, defaults.VitaminB1);
        Assert.Equal(10d, defaults.VitaminD);
        Assert.Equal(1.4, defaults.Omega3);

        var known = await service.SaveIntakeAsync(new IntakeDraft(today, "  鸡胸肉  ", 200, NutritionUnits.Gram, null));
        Assert.Equal(IntakeSaveKind.Saved, known.Kind);

        var wrongUnit = await service.SaveIntakeAsync(new IntakeDraft(today, "牛奶", 200, NutritionUnits.Gram, null));
        Assert.Equal(IntakeSaveKind.Invalid, wrongUnit.Kind);

        var missing = await service.SaveIntakeAsync(new IntakeDraft(today, "试样饮品", 250, NutritionUnits.Milliliter, null));
        Assert.Equal(IntakeSaveKind.NeedChoice, missing.Kind);
        Assert.DoesNotContain((await service.GetDayAsync(today)).Entries, item => item.FoodName == "试样饮品");

        var defined = await service.SaveIntakeAsync(new IntakeDraft(
            today,
            "试样饮品",
            250,
            NutritionUnits.Milliliter,
            new NutrientValues(46, 0.1, 0.1, 11.0)));
        Assert.Equal(IntakeSaveKind.Saved, defined.Kind);

        var again = await service.SaveIntakeAsync(new IntakeDraft(today, "试样饮品", 100, NutritionUnits.Milliliter, null));
        Assert.Equal(IntakeSaveKind.Saved, again.Kind);

        var otherDay = await service.SaveIntakeAsync(new IntakeDraft(tomorrow, "橄榄油", 10, NutritionUnits.Milliliter, null));
        Assert.Equal(IntakeSaveKind.Saved, otherDay.Kind);

        var day = await service.GetDayAsync(today);
        Assert.Equal(3, day.Entries.Count);
        Assert.Equal(266d, day.Entries[0].Nutrients.Calories);
        Assert.Equal(130d, day.Entries[0].Nutrients.Sodium);
        Assert.Equal(0.14, day.Entries[0].Nutrients.VitaminB1);
        Assert.Equal(115d, day.Entries[1].Nutrients.Calories);
        Assert.Equal(46d, day.Entries[2].Nutrients.Calories);
        Assert.Equal(427d, day.Current.Calories);

        var next = await service.GetDayAsync(tomorrow);
        Assert.Single(next.Entries);
        Assert.Equal(89.9, next.Current.Calories);
        Assert.Equal(10d, next.Current.Fat);

        await service.DeleteIntakeAsync(day.Entries[0].Id);
        var afterDelete = await service.GetDayAsync(today);
        Assert.Equal(161d, afterDelete.Current.Calories);

        Assert.Equal("目标不能小于 0", await service.SaveTargetsAsync(new TargetDraft(-1, 150, 60, 250)));
        Assert.Null(await service.SaveTargetsAsync(new TargetDraft(1800, 140, 50, 200)));
        var targets = await service.GetTargetsAsync();
        Assert.Equal(1800d, targets.Calories);
        Assert.Equal(140d, targets.Protein);
    }

    [Fact]
    public async Task Adding150GramsOfDriedTofuUsesTheCatalog()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-dougan-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new NutritionService(database);
        var today = WeekCalendar.DayKey(new DateOnly(2026, 10, 8));
        var catalog = NutritionCatalog.BuiltIn.Single(food => food.Name == "豆腐干");
        var expected = catalog.PerHundred.Scale(150);

        var shortName = await service.SaveIntakeAsync(new IntakeDraft(today, "试样干货", 150, NutritionUnits.Gram, null));
        Assert.Equal(IntakeSaveKind.NeedChoice, shortName.Kind);
        Assert.Empty((await service.GetDayAsync(today)).Entries);

        var saved = await service.SaveIntakeAsync(new IntakeDraft(today, "豆腐干", 150, NutritionUnits.Gram, null));
        Assert.Equal(IntakeSaveKind.Saved, saved.Kind);

        var day = await service.GetDayAsync(today);
        var entry = Assert.Single(day.Entries);
        Assert.Equal("豆腐干", entry.FoodName);
        Assert.Equal(150d, entry.Amount);
        Assert.Equal(NutritionUnits.Gram, entry.Unit);
        Assert.Equal(expected, entry.Nutrients);
        Assert.Equal(expected, day.Current);
        await database.CloseAsync();
    }

    [Fact]
    public async Task ReplacesLegacyFactoryTargetsAndKeepsCustomOnes()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-targets-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new NutritionService(database);
        Assert.Null(await service.SaveTargetsAsync(new TargetDraft(
            2000, 150, 60, 250, 25, 2000, 800, 15, 10, 2000, 1.4, 100, 10, 1.6)));

        await database.InitializeAsync();
        var upgraded = await service.GetTargetsAsync();
        Assert.Equal(2050d, upgraded.Calories);
        Assert.Equal(120d, upgraded.Protein);
        Assert.Equal(57d, upgraded.Fat);
        Assert.Equal(250d, upgraded.Carbs);
        Assert.Equal(1500d, upgraded.Sodium);
        Assert.Equal(12d, upgraded.Iron);

        Assert.Null(await service.SaveTargetsAsync(new TargetDraft(2100, 130, 57, 250)));
        await database.InitializeAsync();
        var custom = await service.GetTargetsAsync();
        Assert.Equal(2100d, custom.Calories);
        Assert.Equal(130d, custom.Protein);
    }

    [Fact]
    public void BuiltInFoodsAreNamedOnceAndWithinLimits()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        Assert.NotEmpty(NutritionCatalog.BuiltIn);
        foreach (var food in NutritionCatalog.BuiltIn)
        {
            Assert.True(names.Add(food.Name), food.Name);
            Assert.True(food.Name.Length is > 0 and <= 40);
            Assert.True(NutritionUnits.IsKnown(food.Unit));
            Assert.Null(NutritionMath.ValidatePerHundred(food.PerHundred));
        }

        Assert.Equal(2, NutritionMath.SharedRunLength("猪肉丝", "瘦肉丝"));
        Assert.Equal(2, NutritionMath.SharedRunLength("猪肉丝", "肉丝"));
        Assert.Equal(1, NutritionMath.SharedRunLength("肉", "肉丝"));
        Assert.False(NutritionMath.IsApproximateName("肉丝", "肉丝"));
        Assert.False(NutritionMath.IsApproximateName("肉", "肉丝"));
        Assert.True(NutritionMath.IsApproximateName("猪肉丝", "瘦肉丝"));

        Assert.Contains(NutritionCatalog.BuiltIn, food => food.Name == "粳米");
        Assert.Contains(NutritionCatalog.BuiltIn, food => food.Name == "家常红烧肉");
        Assert.DoesNotContain(NutritionCatalog.BuiltIn, food => food.Name == "沙县小吃");
    }

    [Fact]
    public async Task UnknownShreddedPorkOffersSimilarFoods()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-similar-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new NutritionService(database);
        var today = WeekCalendar.DayKey(new DateOnly(2026, 10, 8));

        var defined = await service.SaveIntakeAsync(new IntakeDraft(
            today,
            "瘦肉丝",
            80,
            NutritionUnits.Gram,
            new NutrientValues(143, 20.3, 6.2, 1.5)));
        Assert.Equal(IntakeSaveKind.Saved, defined.Kind);

        var choice = await service.SaveIntakeAsync(new IntakeDraft(today, "猪肉丝", 100, NutritionUnits.Gram, null));
        Assert.Equal(IntakeSaveKind.NeedChoice, choice.Kind);
        Assert.Equal("列表里没有「猪肉丝」，要新增吗？", choice.Message);
        Assert.True(choice.Similar.Count is > 0 and <= NutritionMath.ApproximateLimit);
        Assert.All(choice.Similar, item => Assert.Equal(NutritionUnits.Gram, item.Unit));
        Assert.Contains(choice.Similar, item => item.Name == "瘦肉丝");
        Assert.Contains(choice.Similar, item => item.Name == "肉丝");
        var names = choice.Similar.Select(item => item.Name).ToList();
        Assert.True(names.IndexOf("瘦肉丝") < names.IndexOf("肉丝"));
        Assert.DoesNotContain((await service.GetDayAsync(today)).Entries, item => item.FoodName == "猪肉丝");

        var used = await service.SaveIntakeAsync(new IntakeDraft(today, "肉丝", 100, NutritionUnits.Gram, null));
        Assert.Equal(IntakeSaveKind.Saved, used.Kind);
        await database.CloseAsync();
    }

    [Fact]
    public async Task RecordsSunExposureAsVitaminDOnly()
    {
        Assert.Equal(25.13, SunExposure.VitaminDMicrograms(15, 0.25, SunExposure.Summer));
        Assert.Equal(15.08, SunExposure.VitaminDMicrograms(15, 0.25, SunExposure.Spring));
        Assert.Equal(12.56, SunExposure.VitaminDMicrograms(15, 0.25, SunExposure.Autumn));
        Assert.Equal(5.03, SunExposure.VitaminDMicrograms(15, 0.25, SunExposure.Winter));
        Assert.NotNull(SunExposure.Validate(0, 0.25, SunExposure.Summer));
        Assert.NotNull(SunExposure.Validate(15, 0.5, SunExposure.Summer));
        Assert.NotNull(SunExposure.Validate(15, 0.25, 0.3));

        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-sun-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new NutritionService(database);
        var today = WeekCalendar.DayKey(new DateOnly(2026, 10, 8));

        Assert.Null(await service.SaveSunIntakeAsync(today, 15, 0.25, SunExposure.Summer));
        var day = await service.GetDayAsync(today);
        var entry = Assert.Single(day.Entries);
        Assert.Equal(SunExposure.Name, entry.FoodName);
        Assert.Equal(15d, entry.Amount);
        Assert.Equal(NutritionUnits.Minute, entry.Unit);
        Assert.Equal("分钟", NutritionMath.UnitLabel(entry.Unit));
        Assert.Equal(25.13, entry.Nutrients.VitaminD);
        Assert.Equal(0d, entry.Nutrients.Calories);
        Assert.Equal(25.13, day.Current.VitaminD);
        Assert.Equal(0d, day.Current.Protein);
        await database.CloseAsync();
    }

    [Fact]
    public async Task RecordsSupplementServingsApartFromNutrients()
    {
        Assert.Equal(8, SupplementCatalog.Vitamins.Count);
        Assert.Single(SupplementCatalog.Proteins);
        Assert.Equal(5, SupplementCatalog.Fitness.Count);
        Assert.Equal(1.2, SupplementCatalog.Find("b1")!.PerServing.VitaminB1);
        Assert.Equal(100d, SupplementCatalog.Find("c")!.PerServing.VitaminC);
        Assert.Equal(10d, SupplementCatalog.Find("d")!.PerServing.VitaminD);
        Assert.Equal(1.4, SupplementCatalog.Find("multi")!.PerServing.VitaminB1);
        Assert.Equal(100d, SupplementCatalog.Find("multi")!.PerServing.VitaminC);
        Assert.Equal(10d, SupplementCatalog.Find("multi")!.PerServing.VitaminD);
        Assert.Equal(100d, SupplementCatalog.Find("multi")!.PerServing.Calcium);
        Assert.Equal(5d, SupplementCatalog.Find("multi")!.PerServing.Iron);
        Assert.Equal(5d, SupplementCatalog.Find("multi")!.PerServing.Zinc);
        Assert.Equal(200d, SupplementCatalog.Find("calcium")!.PerServing.Calcium);
        Assert.Equal(20d, SupplementCatalog.Find("iron")!.PerServing.Iron);
        Assert.Equal(15d, SupplementCatalog.Find("zinc")!.PerServing.Zinc);
        Assert.Equal(0.3, SupplementCatalog.Find("fish-oil")!.PerServing.Omega3);
        Assert.Equal(24d, SupplementCatalog.Find("whey")!.PerServing.Protein);
        Assert.Equal(5d, SupplementCatalog.Find("creatine")!.PerServingDose.Creatine);
        Assert.Equal(7d, SupplementCatalog.Find("bcaa")!.PerServingDose.Bcaa);
        Assert.Equal(5d, SupplementCatalog.Find("glutamine")!.PerServingDose.Glutamine);
        Assert.Equal(500d, SupplementCatalog.Find("carnitine")!.PerServingDose.Carnitine);
        Assert.Equal(1d, SupplementCatalog.Find("electrolyte")!.PerServingDose.Electrolyte);

        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-supplement-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new NutritionService(database);
        var today = WeekCalendar.DayKey(new DateOnly(2026, 10, 8));

        Assert.Equal("没有这个补剂", await service.SaveSupplementAsync(today, "missing", 1));
        Assert.Equal("份数要大于 0，并且不超过 100", await service.SaveSupplementAsync(today, "creatine", 0));

        Assert.Null(await service.SaveSupplementAsync(today, "c", 2));
        Assert.Null(await service.SaveSupplementAsync(today, "creatine", 2));
        Assert.Null(await service.SaveSupplementAsync(today, "whey", 1));
        Assert.Null(await service.SaveSupplementAsync(today, "bcaa", 1));
        Assert.Null(await service.SaveSupplementAsync(today, "glutamine", 1));
        Assert.Null(await service.SaveSupplementAsync(today, "carnitine", 1));
        Assert.Null(await service.SaveSupplementAsync(today, "electrolyte", 1));

        var day = await service.GetDayAsync(today);
        Assert.Equal(7, day.Entries.Count);
        Assert.Equal(200d, day.Current.VitaminC);
        Assert.Equal(24d, day.Current.Protein);
        Assert.Equal(10d, day.Supplements.Creatine);
        Assert.Equal(7d, day.Supplements.Bcaa);
        Assert.Equal(5d, day.Supplements.Glutamine);
        Assert.Equal(500d, day.Supplements.Carnitine);
        Assert.Equal(1d, day.Supplements.Electrolyte);
        Assert.Equal(0d, day.Entries[0].Supplements.Creatine);
        Assert.Equal(NutritionUnits.Serving, day.Entries[1].Unit);
        Assert.Equal("份", NutritionMath.UnitLabel(day.Entries[1].Unit));
        Assert.Equal(0d, day.Entries[1].Nutrients.Calories);
        await database.CloseAsync();
    }
}
