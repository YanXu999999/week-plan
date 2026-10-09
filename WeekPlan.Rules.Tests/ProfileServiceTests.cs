using WeekPlan;
using Xunit;

namespace WeekPlan.Rules.Tests;

public class ProfileServiceTests
{
    [Fact]
    public async Task SavesAgeAndHeightThenReadsThemBack()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-profile-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new ProfileService(database);

        var empty = await service.GetAsync();
        Assert.Null(empty.Age);
        Assert.Null(empty.HeightCm);

        Assert.Equal("年龄要在 1 到 120 岁之间", await service.SaveAsync(new ProfileDraft(0, 170)));
        Assert.Equal("身高最多保留一位小数", await service.SaveAsync(new ProfileDraft(28, 170.25)));
        Assert.Equal("身高要在 50 到 250 厘米之间", await service.SaveAsync(new ProfileDraft(28, 49.9)));
        Assert.Null(await service.SaveAsync(new ProfileDraft(28, 170.5)));

        var saved = await service.GetAsync();
        Assert.Equal(28, saved.Age);
        Assert.Equal(170.5, saved.HeightCm);
        Assert.Equal(BodySex.Male, saved.Sex);
        Assert.Equal(NutritionGoal.Keep, saved.Goal);

        await service.ToggleSexAsync();
        Assert.Equal(BodySex.Female, (await service.GetAsync()).Sex);
        await service.SaveGoalAsync(NutritionGoal.Cut);
        Assert.True(NutritionGoal.IsCut((await service.GetAsync()).Goal));

        Assert.Null(await service.SaveAsync(new ProfileDraft(30, 171)));
        var updated = await service.GetAsync();
        Assert.Equal(30, updated.Age);
        Assert.Equal(171d, updated.HeightCm);
        await database.CloseAsync();
    }

    [Fact]
    public async Task SavesAgeWithoutRequiringHeight()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-profile-age-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new ProfileService(database);

        Assert.Equal("年龄要在 1 到 120 岁之间", await service.SaveAgeAsync(0));
        Assert.Null(await service.SaveAgeAsync(28));
        var ageOnly = await service.GetAsync();
        Assert.Equal(28, ageOnly.Age);
        Assert.Null(ageOnly.HeightCm);

        Assert.Null(await service.SaveHeightAsync(170.5));
        var both = await service.GetAsync();
        Assert.Equal(28, both.Age);
        Assert.Equal(170.5, both.HeightCm);
        await database.CloseAsync();
    }
}
