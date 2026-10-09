using SQLite;

namespace WeekPlan;

[Table("tasks")]
public sealed class TaskEntity
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    public int DayOfWeek { get; set; }

    public string Type { get; set; } = PlanCodes.Strength;

    public string Title { get; set; } = "";

    public int Sets { get; set; } = PlanLimits.DefaultSets;

    public int RestSeconds { get; set; } = PlanLimits.DefaultRestSeconds;

    public int DurationMinutes { get; set; } = PlanLimits.DefaultDurationMinutes;

    public int SortOrder { get; set; }
}

[Table("nutrition_targets")]
public sealed class NutritionTargetEntity
{
    [PrimaryKey]
    public long Id { get; set; } = 1;

    public double Calories { get; set; } = NutritionTargets.DefaultCalories;

    public double Protein { get; set; } = NutritionTargets.DefaultProtein;

    public double Fat { get; set; } = NutritionTargets.DefaultFat;

    public double Carbs { get; set; } = NutritionTargets.DefaultCarbs;

    public double Fiber { get; set; } = NutritionTargets.DefaultFiber;

    public double Sodium { get; set; } = NutritionTargets.DefaultSodium;

    public double Calcium { get; set; } = NutritionTargets.DefaultCalcium;

    public double Iron { get; set; } = NutritionTargets.DefaultIron;

    public double Zinc { get; set; } = NutritionTargets.DefaultZinc;

    public double Potassium { get; set; } = NutritionTargets.DefaultPotassium;

    public double VitaminB1 { get; set; } = NutritionTargets.DefaultVitaminB1;

    public double VitaminC { get; set; } = NutritionTargets.DefaultVitaminC;

    public double VitaminD { get; set; } = NutritionTargets.DefaultVitaminD;

    public double Omega3 { get; set; } = NutritionTargets.DefaultOmega3;
}

[Table("foods")]
public sealed class FoodEntity
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    [Indexed(Unique = true)]
    public string NameKey { get; set; } = "";

    public string Name { get; set; } = "";

    public string Unit { get; set; } = NutritionUnits.Gram;

    public double Calories { get; set; }

    public double Protein { get; set; }

    public double Fat { get; set; }

    public double Carbs { get; set; }

    public double Fiber { get; set; }

    public double Sodium { get; set; }

    public double Calcium { get; set; }

    public double Iron { get; set; }

    public double Zinc { get; set; }

    public double Potassium { get; set; }

    public double VitaminB1 { get; set; }

    public double VitaminC { get; set; }

    public double VitaminD { get; set; }

    public double Omega3 { get; set; }

    public bool BuiltIn { get; set; }
}

[Table("intakes")]
public sealed class IntakeEntity
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    [Indexed]
    public long EpochDay { get; set; }

    public long FoodId { get; set; }

    public string FoodName { get; set; } = "";

    public double Amount { get; set; }

    public string Unit { get; set; } = NutritionUnits.Gram;

    public double Calories { get; set; }

    public double Protein { get; set; }

    public double Fat { get; set; }

    public double Carbs { get; set; }

    public double Fiber { get; set; }

    public double Sodium { get; set; }

    public double Calcium { get; set; }

    public double Iron { get; set; }

    public double Zinc { get; set; }

    public double Potassium { get; set; }

    public double VitaminB1 { get; set; }

    public double VitaminC { get; set; }

    public double VitaminD { get; set; }

    public double Omega3 { get; set; }

    public double Creatine { get; set; }

    public double Bcaa { get; set; }

    public double Glutamine { get; set; }

    public double Carnitine { get; set; }

    public double Electrolyte { get; set; }
}

[Table("profile")]
public sealed class ProfileEntity
{
    [PrimaryKey]
    public long Id { get; set; } = 1;

    public int? Age { get; set; }

    public double? HeightCm { get; set; }

    public string Sex { get; set; } = BodySex.Male;

    public string Goal { get; set; } = NutritionGoal.Keep;
}

[Table("weights")]
public sealed class WeightEntity
{
    [PrimaryKey]
    public long EpochDay { get; set; }

    public double Kilograms { get; set; }
}

[Table("body_measures")]
public sealed class BodyMeasureEntity
{
    [PrimaryKey]
    public long EpochDay { get; set; }

    public double WaistCm { get; set; }

    public double LeftArmCm { get; set; }

    public double RightArmCm { get; set; }

    public double ChestCm { get; set; }

    public double LeftThighCm { get; set; }

    public double RightThighCm { get; set; }
}

[Table("app_flags")]
public sealed class AppFlagsEntity
{
    [PrimaryKey]
    public long Id { get; set; } = 1;

    public long EmptyBodyReminderMonday { get; set; }

    public int CatalogRevision { get; set; }
}

[Table("task_logs")]
public sealed class TaskLogEntity
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    [Indexed]
    public long TaskId { get; set; }

    public long EpochDay { get; set; }

    public string Status { get; set; } = PlanCodes.NotStarted;

    public int CurrentSet { get; set; } = 1;

    public long? RestEndsAt { get; set; }
}

public sealed class AppDatabase
{
    private readonly SQLiteAsyncConnection _connection;

    public AppDatabase(string path)
    {
        _connection = new SQLiteAsyncConnection(path);
    }

    public SQLiteAsyncConnection Connection => _connection;

    public async Task InitializeAsync()
    {
        await _connection.CreateTableAsync<TaskEntity>().ConfigureAwait(false);
        await _connection.CreateTableAsync<TaskLogEntity>().ConfigureAwait(false);
        await _connection.ExecuteAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS ix_task_logs_task_day ON task_logs(TaskId, EpochDay);").ConfigureAwait(false);
        await _connection.ExecuteAsync(
            "UPDATE tasks SET Type = ? WHERE Type = ?",
            PlanCodes.Strength,
            PlanCodes.Fitness).ConfigureAwait(false);
        await _connection.ExecuteAsync(
            "DELETE FROM task_logs WHERE TaskId IN (SELECT Id FROM tasks WHERE Type = ?)",
            "DIET").ConfigureAwait(false);
        await _connection.ExecuteAsync(
            "DELETE FROM tasks WHERE Type = ?",
            "DIET").ConfigureAwait(false);
        await _connection.CreateTableAsync<NutritionTargetEntity>().ConfigureAwait(false);
        await _connection.CreateTableAsync<FoodEntity>().ConfigureAwait(false);
        await _connection.CreateTableAsync<IntakeEntity>().ConfigureAwait(false);
        await _connection.CreateTableAsync<ProfileEntity>().ConfigureAwait(false);
        await EnsureProfileColumnsAsync().ConfigureAwait(false);
        await _connection.CreateTableAsync<WeightEntity>().ConfigureAwait(false);
        await _connection.CreateTableAsync<BodyMeasureEntity>().ConfigureAwait(false);
        await _connection.CreateTableAsync<AppFlagsEntity>().ConfigureAwait(false);
        await EnsureNutritionAsync().ConfigureAwait(false);
    }

    private static bool IsLegacyFactoryTarget(NutritionTargetEntity row)
    {
        if (!Near(row.Calories, 2000) || !Near(row.Protein, 150) || !Near(row.Fat, 60) || !Near(row.Carbs, 250))
            return false;
        var unset = Near(row.Fiber, 0) && Near(row.Sodium, 0) && Near(row.Calcium, 0) && Near(row.Iron, 0)
            && Near(row.Zinc, 0) && Near(row.Potassium, 0) && Near(row.VitaminB1, 0) && Near(row.VitaminC, 0)
            && Near(row.VitaminD, 0) && Near(row.Omega3, 0);
        var previous = Near(row.Fiber, 25) && Near(row.Sodium, 2000) && Near(row.Calcium, 800) && Near(row.Iron, 15)
            && Near(row.Zinc, 10) && Near(row.Potassium, 2000) && Near(row.VitaminB1, 1.4) && Near(row.VitaminC, 100)
            && Near(row.VitaminD, 10) && Near(row.Omega3, 1.6);
        return unset || previous;
    }

    private static bool Near(double actual, double expected) => Math.Abs(actual - expected) < 0.001;

    private static readonly string[] ExtraNutrientColumns =
    {
        "Fiber", "Sodium", "Calcium", "Iron", "Zinc", "Potassium", "VitaminB1", "VitaminC", "VitaminD", "Omega3",
    };

    private static readonly string[] SupplementColumns =
    {
        "Creatine", "Bcaa", "Glutamine", "Carnitine", "Electrolyte",
    };

    private async Task EnsureNutritionAsync()
    {
        var targetsAdded = await EnsureNutrientColumnsAsync("nutrition_targets").ConfigureAwait(false);
        await EnsureNutrientColumnsAsync("foods").ConfigureAwait(false);
        var intakesAdded = await EnsureNutrientColumnsAsync("intakes").ConfigureAwait(false);
        await EnsureSupplementColumnsAsync().ConfigureAwait(false);
        var flags = await _connection.FindAsync<AppFlagsEntity>(1L).ConfigureAwait(false);
        var catalogReady = flags?.CatalogRevision == NutritionCatalog.Revision;

        var target = await _connection.FindAsync<NutritionTargetEntity>(1L).ConfigureAwait(false);
        if (target is null)
        {
            var created = new NutritionTargetEntity { Id = 1 };
            NutrientIo.Apply(created, new TargetDraft(
                NutritionTargets.DefaultCalories,
                NutritionTargets.DefaultProtein,
                NutritionTargets.DefaultFat,
                NutritionTargets.DefaultCarbs));
            await _connection.InsertAsync(created).ConfigureAwait(false);
        }
        else if (IsLegacyFactoryTarget(target))
        {
            NutrientIo.Apply(target, new TargetDraft(
                NutritionTargets.DefaultCalories,
                NutritionTargets.DefaultProtein,
                NutritionTargets.DefaultFat,
                NutritionTargets.DefaultCarbs));
            await _connection.UpdateAsync(target).ConfigureAwait(false);
        }
        else if (targetsAdded)
        {
            await _connection.ExecuteAsync(
                """
                UPDATE nutrition_targets
                SET Fiber = ?, Sodium = ?, Calcium = ?, Iron = ?, Zinc = ?, Potassium = ?,
                    VitaminB1 = ?, VitaminC = ?, VitaminD = ?, Omega3 = ?
                WHERE Id = 1
                """,
                NutritionTargets.DefaultFiber,
                NutritionTargets.DefaultSodium,
                NutritionTargets.DefaultCalcium,
                NutritionTargets.DefaultIron,
                NutritionTargets.DefaultZinc,
                NutritionTargets.DefaultPotassium,
                NutritionTargets.DefaultVitaminB1,
                NutritionTargets.DefaultVitaminC,
                NutritionTargets.DefaultVitaminD,
                NutritionTargets.DefaultOmega3).ConfigureAwait(false);
        }

        if (!catalogReady)
            await SyncBuiltInFoodsAsync().ConfigureAwait(false);

        if (intakesAdded)
            await BackfillIntakeMicrosAsync().ConfigureAwait(false);

        if (!catalogReady)
        {
            flags ??= new AppFlagsEntity { Id = 1 };
            flags.CatalogRevision = NutritionCatalog.Revision;
            await _connection.InsertOrReplaceAsync(flags).ConfigureAwait(false);
        }
    }

    private Task SyncBuiltInFoodsAsync()
    {
        return _connection.RunInTransactionAsync(connection =>
        {
            var byKey = connection.Table<FoodEntity>().ToList()
                .GroupBy(item => item.NameKey)
                .ToDictionary(group => group.Key, group => group.First());
            foreach (var food in NutritionCatalog.BuiltIn)
            {
                var key = NutritionMath.NameKey(food.Name);
                if (!byKey.TryGetValue(key, out var existing))
                {
                    var created = new FoodEntity
                    {
                        NameKey = key,
                        Name = food.Name,
                        Unit = food.Unit,
                        BuiltIn = true,
                    };
                    NutrientIo.Apply(created, food.PerHundred);
                    connection.Insert(created);
                    byKey[key] = created;
                    continue;
                }

                if (!existing.BuiltIn)
                    continue;
                NutrientIo.ApplyMicros(existing, food.PerHundred);
                connection.Update(existing);
            }
        });
    }

    private async Task EnsureProfileColumnsAsync()
    {
        await AddTextColumnIfMissingAsync("profile", "Sex", BodySex.Male).ConfigureAwait(false);
        await AddTextColumnIfMissingAsync("profile", "Goal", NutritionGoal.Keep).ConfigureAwait(false);
    }

    private async Task AddTextColumnIfMissingAsync(string table, string column, string fallback)
    {
        var columns = await _connection.QueryAsync<ColumnInfo>($"PRAGMA table_info({table})").ConfigureAwait(false);
        if (columns.Any(item => string.Equals(item.Name, column, StringComparison.OrdinalIgnoreCase)))
            return;
        await _connection.ExecuteAsync(
            $"ALTER TABLE {table} ADD COLUMN {column} TEXT NOT NULL DEFAULT '{fallback}'").ConfigureAwait(false);
    }

    private async Task EnsureSupplementColumnsAsync()
    {
        foreach (var column in SupplementColumns)
            await AddColumnIfMissingAsync("intakes", column).ConfigureAwait(false);
    }

    private async Task<bool> EnsureNutrientColumnsAsync(string table)
    {
        var added = false;
        foreach (var column in ExtraNutrientColumns)
            added |= await AddColumnIfMissingAsync(table, column).ConfigureAwait(false);
        return added;
    }

    private async Task<bool> AddColumnIfMissingAsync(string table, string column)
    {
        var columns = await _connection.QueryAsync<ColumnInfo>($"PRAGMA table_info({table})").ConfigureAwait(false);
        if (columns.Any(item => string.Equals(item.Name, column, StringComparison.OrdinalIgnoreCase)))
            return false;
        await _connection.ExecuteAsync($"ALTER TABLE {table} ADD COLUMN {column} REAL NOT NULL DEFAULT 0").ConfigureAwait(false);
        return true;
    }

    private async Task BackfillIntakeMicrosAsync()
    {
        var foods = await _connection.Table<FoodEntity>().ToListAsync().ConfigureAwait(false);
        var byId = foods.ToDictionary(food => food.Id);
        var intakes = await _connection.Table<IntakeEntity>().ToListAsync().ConfigureAwait(false);
        foreach (var intake in intakes)
        {
            if (!byId.TryGetValue(intake.FoodId, out var food))
                continue;
            NutrientIo.ApplyMicros(intake, NutrientIo.FromFood(food).Scale(intake.Amount));
            await _connection.UpdateAsync(intake).ConfigureAwait(false);
        }
    }

    private sealed class ColumnInfo
    {
        [Column("name")]
        public string Name { get; set; } = "";
    }

    public Task CloseAsync() => _connection.CloseAsync();
}
