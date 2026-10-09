using WeekPlan;
using Xunit;

namespace WeekPlan.Rules.Tests;

public class NutrientToneTests
{
    [Fact]
    public void ColorsPositiveLimitAndBothGoals()
    {
        Assert.Equal(NutrientTone.Red, Tone(NutrientRole.Positive, false, 16.2, 120, false, null));
        Assert.Equal(NutrientTone.Red, Tone(NutrientRole.Positive, false, 4.9, 12, false, null));
        Assert.Equal(NutrientTone.Green, Tone(NutrientRole.Positive, false, 108, 120, false, null));
        Assert.Equal(NutrientTone.Yellow, Tone(NutrientRole.Positive, false, 70, 120, false, null));
        Assert.Equal(NutrientTone.Green, Tone(NutrientRole.Limit, false, 76, 1500, false, null));
        Assert.Equal(NutrientTone.Yellow, Tone(NutrientRole.Limit, false, 1600, 1500, false, null));
        Assert.Equal(NutrientTone.Red, Tone(NutrientRole.Limit, false, 1700, 1500, false, null));

        Assert.Equal(NutrientTone.Green, Tone(NutrientRole.Range, true, 1750, 2050, true, 1500));
        Assert.Equal(NutrientTone.Yellow, Tone(NutrientRole.Range, true, 1450, 2050, true, 1400));
        Assert.Equal(NutrientTone.Yellow, Tone(NutrientRole.Range, true, 1950, 2050, true, 1500));
        Assert.Equal(NutrientTone.Red, Tone(NutrientRole.Range, true, 140, 2050, true, 1500));
        Assert.Equal(NutrientTone.Red, Tone(NutrientRole.Range, true, 2100, 2050, true, 1500));
        Assert.Equal(NutrientTone.Red, Tone(NutrientRole.Range, true, 1400, 2050, true, 1600));

        Assert.Equal(NutrientTone.Yellow, Tone(NutrientRole.Range, false, 20, 57, true, null));
        Assert.Equal(NutrientTone.Green, Tone(NutrientRole.Range, false, 45, 57, true, null));
        Assert.Equal(NutrientTone.Yellow, Tone(NutrientRole.Range, false, 60, 57, true, null));
        Assert.Equal(NutrientTone.Red, Tone(NutrientRole.Range, false, 70, 57, true, null));

        Assert.Equal(NutrientTone.Red, Tone(NutrientRole.Range, true, 1800, 2050, false, null));
        Assert.Equal(NutrientTone.Green, Tone(NutrientRole.Range, true, 2050, 2050, false, null));
        Assert.Equal(NutrientTone.Yellow, Tone(NutrientRole.Range, true, 2400, 2050, false, null));
        Assert.Equal(NutrientTone.Red, Tone(NutrientRole.Range, true, 2700, 2050, false, null));
    }

    [Fact]
    public void DescribesBasalOrTheMissingPieces()
    {
        Assert.Equal(1649d, MetabolismRules.Basal(30, 175, 70, BodySex.Male));
        Assert.Equal(1483d, MetabolismRules.Basal(30, 175, 70, BodySex.Female));
        Assert.Null(MetabolismRules.Basal(null, 175, 70, BodySex.Male));
        Assert.Equal("缺少年龄、身高、体重", MetabolismRules.Describe(null, null, null, BodySex.Male));
        Assert.Equal("缺少体重", MetabolismRules.Describe(30, 175, null, BodySex.Male));
        Assert.Equal("1649 千卡", MetabolismRules.Describe(30, 175, 70, BodySex.Male));
        Assert.Equal("脂肪", NutritionFact.All.Single(item => item.Energy == false && item.Role == NutrientRole.Range && item.Name != "碳水").Name);
    }

    private static NutrientTone Tone(NutrientRole role, bool energy, double current, double target, bool cutting, double? basal) =>
        NutrientToneRules.Tone(role, energy, current, target, cutting, basal);
}
