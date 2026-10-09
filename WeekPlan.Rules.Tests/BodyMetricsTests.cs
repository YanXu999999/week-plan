using WeekPlan;
using Xunit;

namespace WeekPlan.Rules.Tests;

public class BodyMetricsTests
{
    [Fact]
    public void AveragesTheLatestSevenRecordsIncludingTheNewest()
    {
        var entries = Enumerable.Range(1, 8)
            .Select(day => new WeightEntry(day, 60 + day))
            .ToList();

        Assert.Equal(65d, WeightRules.AverageOfLatest(entries));

        entries[7] = new WeightEntry(8, 75);
        Assert.Equal(66d, WeightRules.AverageOfLatest(entries));
        Assert.Equal(70.2, WeightRules.AverageOfLatest(new[]
        {
            new WeightEntry(1, 70.1),
            new WeightEntry(2, 70.2),
        }));
    }

    [Fact]
    public void RemindsOnTheEmptyWeekThenEverySecondSunday()
    {
        var anchor = new DateOnly(2026, 10, 5);
        Assert.True(BodyReminderRules.ShowEmpty(new DateOnly(2026, 10, 11), anchor));
        Assert.False(BodyReminderRules.ShowEmpty(new DateOnly(2026, 10, 18), anchor));
        Assert.False(BodyReminderRules.ShowEmpty(new DateOnly(2026, 10, 10), anchor));

        var recordedOnSunday = new DateOnly(2026, 10, 11);
        Assert.False(BodyReminderRules.IsScheduledSunday(recordedOnSunday, recordedOnSunday));
        Assert.False(BodyReminderRules.IsScheduledSunday(new DateOnly(2026, 10, 18), recordedOnSunday));
        Assert.True(BodyReminderRules.IsScheduledSunday(new DateOnly(2026, 10, 25), recordedOnSunday));
        Assert.False(BodyReminderRules.IsScheduledSunday(new DateOnly(2026, 11, 1), recordedOnSunday));
        Assert.True(BodyReminderRules.IsScheduledSunday(new DateOnly(2026, 11, 8), recordedOnSunday));

        var recordedOnWednesday = new DateOnly(2026, 10, 7);
        Assert.False(BodyReminderRules.IsScheduledSunday(new DateOnly(2026, 10, 11), recordedOnWednesday));
        Assert.False(BodyReminderRules.IsScheduledSunday(new DateOnly(2026, 10, 18), recordedOnWednesday));
        Assert.True(BodyReminderRules.IsScheduledSunday(new DateOnly(2026, 10, 25), recordedOnWednesday));
    }

    [Fact]
    public void HitsTheNearestChartPoint()
    {
        var anchors = ChartGeometry.Layout(200, 120, new[] { 60d, 70d });
        Assert.Equal(2, anchors.Count);
        Assert.True(anchors[0].Y > anchors[1].Y);
        Assert.Equal(1, ChartGeometry.HitIndex(anchors, anchors[1].X, anchors[1].Y, 22));
        Assert.Equal(-1, ChartGeometry.HitIndex(anchors, 0, 0, 22));
    }

    [Fact]
    public async Task KeepsOneWeightPerDayAndDropsAnythingOlderThan365Days()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-body-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new BodyMetricsService(database);
        var today = new DateOnly(2026, 10, 8);
        var todayEpoch = today.DayNumber;

        Assert.Equal("体重要在 20 到 300 千克之间", await service.SaveWeightAsync(todayEpoch, 19.9, todayEpoch));
        Assert.Equal("体重最多保留一位小数", await service.SaveWeightAsync(todayEpoch, 70.25, todayEpoch));
        Assert.Null(await service.SaveWeightAsync(todayEpoch, 70.1, todayEpoch));
        Assert.Null(await service.SaveWeightAsync(todayEpoch, 70.4, todayEpoch));
        Assert.Null(await service.SaveWeightAsync(today.AddDays(-365).DayNumber, 80, todayEpoch));
        Assert.Null(await service.SaveWeightAsync(today.AddDays(-364).DayNumber, 81, todayEpoch));

        var saved = await service.GetWeightsAsync();
        Assert.Equal(2, saved.Count);
        Assert.Equal(today.AddDays(-364).DayNumber, saved[0].EpochDay);
        Assert.Equal(81d, saved[0].Kilograms);
        Assert.Equal(70.4, saved[1].Kilograms);
        Assert.Equal(75.7, WeightRules.AverageOfLatest(saved));

        await service.DeleteWeightAsync(todayEpoch);
        var afterDelete = await service.GetWeightsAsync();
        Assert.Single(afterDelete);
        Assert.Equal(81d, afterDelete[0].Kilograms);
        await database.CloseAsync();
    }

    [Fact]
    public async Task BodyReminderStaysOnTheFirstEmptyWeekUntilARecordExists()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-remind-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new BodyMetricsService(database);
        var today = new DateOnly(2026, 10, 8);
        var thisSunday = new DateOnly(2026, 10, 11);
        var nextSunday = new DateOnly(2026, 10, 18);

        Assert.True(await service.ShowReminderAsync(thisSunday, today, null));
        Assert.False(await service.ShowReminderAsync(nextSunday, nextSunday, null));
        Assert.True(await service.ShowReminderAsync(thisSunday, today, null));

        var draft = new BodyMeasureDraft(80, 30, 30.5, 95, 55, 55.5);
        Assert.Equal("腰围最多保留一位小数", await service.SaveMeasuresAsync(today.DayNumber, draft with { WaistCm = 80.25 }));
        Assert.Null(await service.SaveMeasuresAsync(today.DayNumber, draft));
        Assert.Null(await service.SaveMeasuresAsync(today.DayNumber, draft with { ChestCm = 96 }));
        var measures = await service.GetMeasuresAsync();
        Assert.Single(measures);
        Assert.Equal(96d, measures[0].ChestCm);
        Assert.Equal(80d, measures[0].WaistCm);

        Assert.False(await service.ShowReminderAsync(thisSunday, today, measures[^1]));
        Assert.True(await service.ShowReminderAsync(new DateOnly(2026, 10, 25), today, measures[^1]));
        Assert.False(await service.ShowReminderAsync(new DateOnly(2026, 11, 1), today, measures[^1]));
        await database.CloseAsync();
    }
}
