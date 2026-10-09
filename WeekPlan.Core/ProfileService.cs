namespace WeekPlan;

public sealed class ProfileService
{
    private readonly AppDatabase _database;

    public ProfileService(AppDatabase database)
    {
        _database = database;
    }

    public async Task<BodyProfile> GetAsync()
    {
        var row = await _database.Connection.FindAsync<ProfileEntity>(1L).ConfigureAwait(false);
        return row is null
            ? BodyProfile.Empty
            : new BodyProfile(row.Age, row.HeightCm, BodySex.Normalize(row.Sex), NutritionGoal.Normalize(row.Goal));
    }

    public async Task<string?> SaveAsync(ProfileDraft draft)
    {
        var error = ProfileRules.Validate(draft);
        if (error is not null)
            return error;

        var row = await FindOrCreateAsync().ConfigureAwait(false);
        row.Age = draft.Age;
        row.HeightCm = ProfileRules.RoundHeight(draft.HeightCm);
        await _database.Connection.InsertOrReplaceAsync(row).ConfigureAwait(false);
        return null;
    }

    public async Task<string?> SaveAgeAsync(int age)
    {
        var error = ProfileRules.ValidateAge(age);
        if (error is not null)
            return error;

        var row = await FindOrCreateAsync().ConfigureAwait(false);
        row.Age = age;
        await _database.Connection.InsertOrReplaceAsync(row).ConfigureAwait(false);
        return null;
    }

    public async Task<string?> SaveHeightAsync(double heightCm)
    {
        var error = ProfileRules.ValidateHeight(heightCm);
        if (error is not null)
            return error;

        var row = await FindOrCreateAsync().ConfigureAwait(false);
        row.HeightCm = ProfileRules.RoundHeight(heightCm);
        await _database.Connection.InsertOrReplaceAsync(row).ConfigureAwait(false);
        return null;
    }

    public async Task ToggleSexAsync()
    {
        var row = await FindOrCreateAsync().ConfigureAwait(false);
        row.Sex = BodySex.Normalize(row.Sex) == BodySex.Female ? BodySex.Male : BodySex.Female;
        await _database.Connection.InsertOrReplaceAsync(row).ConfigureAwait(false);
    }

    public async Task SaveGoalAsync(string goal)
    {
        var row = await FindOrCreateAsync().ConfigureAwait(false);
        row.Goal = NutritionGoal.Normalize(goal);
        await _database.Connection.InsertOrReplaceAsync(row).ConfigureAwait(false);
    }

    private async Task<ProfileEntity> FindOrCreateAsync()
    {
        var row = await _database.Connection.FindAsync<ProfileEntity>(1L).ConfigureAwait(false);
        return row ?? new ProfileEntity { Id = 1 };
    }
}
