using WeekPlan;
using Xunit;

namespace WeekPlan.Rules.Tests;

public class WeekPlanServiceTests
{
    [Fact]
    public async Task FitnessRestStaysOnItsOwnDay()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new WeekPlanService(database);
        var today = new DateOnly(2026, 10, 8);
        var day = WeekCalendar.PlanDay(today);
        var key = WeekCalendar.DayKey(today);

        Assert.True(await service.SaveAsync(new TaskDraft(null, day, key, PlanCodes.Fitness, "深蹲", 3, 60, null)));

        var items = await service.GetDayAsync(day, key);
        var fitness = Assert.Single(items);
        Assert.Equal(PlanCodes.Strength, fitness.Type);
        await service.SetFitnessStatusAsync(fitness.Id, key, PlanCodes.InProgress, 0);
        await service.CompleteSetAsync(fitness.Id, key, 10_000);

        items = await service.GetDayAsync(day, key);
        var training = items.Single(item => item.Id == fitness.Id);
        Assert.Equal(PlanCodes.InProgress, training.Status);
        Assert.Equal(70_000, training.RestEndsAt);

        await service.ChangeRestAsync(fitness.Id, 90);
        items = await service.GetDayAsync(day, key);
        training = items.Single(item => item.Id == fitness.Id);
        Assert.Equal(90, training.RestSeconds);
        Assert.Equal(70_000, training.RestEndsAt);

        var nextWeek = await service.GetDayAsync(day, WeekCalendar.DayKey(today.AddDays(7)));
        Assert.Equal(PlanCodes.NotStarted, nextWeek.Single(item => item.Id == fitness.Id).Status);

        await database.CloseAsync();
        File.Delete(path);
    }

    [Fact]
    public async Task DietTasksAreRemovedOnInitialize()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var today = new DateOnly(2026, 10, 8);
        var day = WeekCalendar.PlanDay(today);
        await database.Connection.InsertAsync(new TaskEntity
        {
            DayOfWeek = day,
            Type = "DIET",
            Title = "鸡胸肉",
            Sets = 1,
            RestSeconds = 60,
            SortOrder = 1,
        });
        var diet = Assert.Single(await database.Connection.Table<TaskEntity>().ToListAsync());
        await database.Connection.InsertAsync(new TaskLogEntity
        {
            TaskId = diet.Id,
            EpochDay = WeekCalendar.DayKey(today),
            Status = PlanCodes.Done,
        });

        await database.InitializeAsync();
        Assert.Empty(await database.Connection.Table<TaskEntity>().ToListAsync());
        Assert.Empty(await database.Connection.Table<TaskLogEntity>().ToListAsync());

        await database.CloseAsync();
        File.Delete(path);
    }

    [Fact]
    public async Task CardioCountdownStartsOnCommandAndFinishesAtEnd()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var service = new WeekPlanService(database);
        var today = new DateOnly(2026, 10, 8);
        var day = WeekCalendar.PlanDay(today);
        var key = WeekCalendar.DayKey(today);

        Assert.True(await service.SaveAsync(new TaskDraft(null, day, key, PlanCodes.Cardio, "跑步", 3, 60, null, 30)));
        var cardio = (await service.GetDayAsync(day, key)).Single();
        Assert.Equal(30, cardio.DurationMinutes);
        Assert.Equal(PlanCodes.NotStarted, cardio.Status);
        Assert.Null(cardio.RestEndsAt);

        var start = 1_000_000L;
        await service.SetFitnessStatusAsync(cardio.Id, key, PlanCodes.InProgress, start);
        cardio = (await service.GetDayAsync(day, key)).Single();
        Assert.Equal(PlanCodes.InProgress, cardio.Status);
        Assert.Equal(start + 30 * 60_000L, cardio.RestEndsAt);

        await service.SetFitnessStatusAsync(cardio.Id, key, PlanCodes.InProgress, start + 5_000);
        cardio = (await service.GetDayAsync(day, key)).Single();
        Assert.Equal(start + 30 * 60_000L, cardio.RestEndsAt);

        await service.SyncRestsAsync(key, start + 30 * 60_000L - 1);
        cardio = (await service.GetDayAsync(day, key)).Single();
        Assert.Equal(PlanCodes.InProgress, cardio.Status);

        await service.SyncRestsAsync(key, start + 30 * 60_000L);
        cardio = (await service.GetDayAsync(day, key)).Single();
        Assert.Equal(PlanCodes.Done, cardio.Status);
        Assert.Null(cardio.RestEndsAt);

        var session = await service.GetSessionAsync(cardio.Id, key);
        Assert.NotNull(session);
        Assert.Equal(PlanCodes.Cardio, session.Type);
        Assert.Equal(30, session.DurationMinutes);
        Assert.Equal(PlanCodes.Done, session.Status);

        await database.CloseAsync();
        File.Delete(path);
    }

    [Fact]
    public async Task LegacyFitnessRowsBecomeStrength()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(Path.GetTempPath(), $"weekplan-{Guid.NewGuid():N}.db");
        var database = new AppDatabase(path);
        await database.InitializeAsync();
        var today = new DateOnly(2026, 10, 8);
        var day = WeekCalendar.PlanDay(today);
        await database.Connection.InsertAsync(new TaskEntity
        {
            DayOfWeek = day,
            Type = PlanCodes.Fitness,
            Title = "卧推",
            Sets = 4,
            RestSeconds = 90,
            SortOrder = 1,
        });

        await database.InitializeAsync();
        var stored = await database.Connection.Table<TaskEntity>().ToListAsync();
        Assert.Equal(PlanCodes.Strength, Assert.Single(stored).Type);
        Assert.Equal(4, stored[0].Sets);

        await database.CloseAsync();
        File.Delete(path);
    }
}
