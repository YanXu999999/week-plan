namespace WeekPlan;

public sealed class BodyMetricsService
{
    private readonly AppDatabase _database;

    public BodyMetricsService(AppDatabase database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<WeightEntry>> GetWeightsAsync()
    {
        var rows = await _database.Connection.QueryAsync<WeightEntity>(
            "SELECT * FROM weights ORDER BY EpochDay ASC").ConfigureAwait(false);
        return rows.Select(row => new WeightEntry(row.EpochDay, row.Kilograms)).ToList();
    }

    public async Task<WeightEntry?> FindWeightAsync(long epochDay)
    {
        var row = await _database.Connection.FindAsync<WeightEntity>(epochDay).ConfigureAwait(false);
        return row is null ? null : new WeightEntry(row.EpochDay, row.Kilograms);
    }

    public async Task<string?> SaveWeightAsync(long epochDay, double kilograms, long todayEpoch)
    {
        var error = WeightRules.Validate(kilograms);
        if (error is not null)
            return error;

        await _database.Connection.InsertOrReplaceAsync(new WeightEntity
        {
            EpochDay = epochDay,
            Kilograms = WeightRules.Round(kilograms),
        }).ConfigureAwait(false);
        await _database.Connection.ExecuteAsync(
            "DELETE FROM weights WHERE EpochDay < ?",
            WeightRules.OldestKeptEpoch(todayEpoch)).ConfigureAwait(false);
        return null;
    }

    public Task DeleteWeightAsync(long epochDay) =>
        _database.Connection.ExecuteAsync("DELETE FROM weights WHERE EpochDay = ?", epochDay);

    public async Task<IReadOnlyList<BodyMeasures>> GetMeasuresAsync()
    {
        var rows = await _database.Connection.QueryAsync<BodyMeasureEntity>(
            "SELECT * FROM body_measures ORDER BY EpochDay ASC").ConfigureAwait(false);
        return rows.Select(Map).ToList();
    }

    public async Task<string?> SaveMeasuresAsync(long epochDay, BodyMeasureDraft draft)
    {
        var error = BodyMeasureRules.Validate(draft);
        if (error is not null)
            return error;

        await _database.Connection.InsertOrReplaceAsync(new BodyMeasureEntity
        {
            EpochDay = epochDay,
            WaistCm = BodyMeasureRules.Round(draft.WaistCm),
            LeftArmCm = BodyMeasureRules.Round(draft.LeftArmCm),
            RightArmCm = BodyMeasureRules.Round(draft.RightArmCm),
            ChestCm = BodyMeasureRules.Round(draft.ChestCm),
            LeftThighCm = BodyMeasureRules.Round(draft.LeftThighCm),
            RightThighCm = BodyMeasureRules.Round(draft.RightThighCm),
        }).ConfigureAwait(false);
        return null;
    }

    public async Task<bool> ShowReminderAsync(DateOnly viewedDate, DateOnly today, BodyMeasures? latest)
    {
        if (latest is null)
        {
            var anchor = await EnsureEmptyAnchorAsync(today).ConfigureAwait(false);
            return viewedDate.DayOfWeek == DayOfWeek.Sunday && BodyReminderRules.ShowEmpty(viewedDate, anchor);
        }

        if (viewedDate.DayOfWeek != DayOfWeek.Sunday)
            return false;
        var lastDate = DateOnly.FromDayNumber(checked((int)latest.EpochDay));
        return BodyReminderRules.IsScheduledSunday(viewedDate, lastDate);
    }

    private async Task<DateOnly> EnsureEmptyAnchorAsync(DateOnly today)
    {
        var row = await _database.Connection.FindAsync<AppFlagsEntity>(1L).ConfigureAwait(false);
        if (row is not null && row.EmptyBodyReminderMonday > 0)
            return DateOnly.FromDayNumber(checked((int)row.EmptyBodyReminderMonday));

        var monday = BodyReminderRules.MondayOf(today);
        var saved = row ?? new AppFlagsEntity { Id = 1 };
        saved.EmptyBodyReminderMonday = monday.DayNumber;
        await _database.Connection.InsertOrReplaceAsync(saved).ConfigureAwait(false);
        return monday;
    }

    private static BodyMeasures Map(BodyMeasureEntity row) => new(
        row.EpochDay,
        row.WaistCm,
        row.LeftArmCm,
        row.RightArmCm,
        row.ChestCm,
        row.LeftThighCm,
        row.RightThighCm);
}
