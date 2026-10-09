namespace WeekPlan;

public sealed class NutritionService
{
    private readonly AppDatabase _database;

    public NutritionService(AppDatabase database)
    {
        _database = database;
    }

    public async Task<NutritionTargets> GetTargetsAsync()
    {
        var row = await _database.Connection.FindAsync<NutritionTargetEntity>(1L).ConfigureAwait(false);
        if (row is null)
            return NutritionTargets.Default;
        return NutrientIo.FromTarget(row);
    }

    public async Task<string?> SaveTargetsAsync(TargetDraft draft)
    {
        var error = NutritionMath.ValidateTargets(draft);
        if (error is not null)
            return error;

        var row = new NutritionTargetEntity { Id = 1 };
        NutrientIo.Apply(row, draft);
        await _database.Connection.InsertOrReplaceAsync(row).ConfigureAwait(false);
        return null;
    }

    public async Task<DayNutrition> GetDayAsync(long epochDay)
    {
        var targets = await GetTargetsAsync().ConfigureAwait(false);
        var rows = await _database.Connection.Table<IntakeEntity>()
            .Where(item => item.EpochDay == epochDay)
            .ToListAsync()
            .ConfigureAwait(false);
        var entries = rows
            .OrderBy(item => item.Id)
            .Select(item => new IntakeEntry(
                item.Id,
                item.EpochDay,
                item.FoodName,
                item.Amount,
                item.Unit,
                NutrientIo.FromIntake(item),
                NutrientIo.Dose(item)))
            .ToList();
        var current = NutrientValues.Zero;
        var supplements = SupplementDose.Zero;
        foreach (var entry in entries)
        {
            current = current.Add(entry.Nutrients);
            supplements = supplements.Add(entry.Supplements);
        }

        return new DayNutrition(epochDay, targets, current, supplements, entries);
    }

    public async Task<string?> SaveSupplementAsync(long epochDay, string id, double servings)
    {
        var item = SupplementCatalog.Find(id);
        if (item is null)
            return "没有这个补剂";
        if (double.IsNaN(servings) || double.IsInfinity(servings) || servings <= 0 || servings > 100)
            return "份数要大于 0，并且不超过 100";

        var intake = new IntakeEntity
        {
            EpochDay = epochDay,
            FoodName = item.Name,
            Amount = servings,
            Unit = NutritionUnits.Serving,
        };
        NutrientIo.Apply(intake, item.PerServing.Multiply(servings));
        NutrientIo.Apply(intake, item.PerServingDose.Multiply(servings));
        await _database.Connection.InsertAsync(intake).ConfigureAwait(false);
        return null;
    }

    public async Task<IntakeSaveResult> SaveIntakeAsync(IntakeDraft draft)
    {
        var name = NutritionMath.NormalizeName(draft.Name);
        if (name.Length == 0)
            return IntakeSaveResult.Invalid("先写上食物名称");
        if (name.Length > 40)
            return IntakeSaveResult.Invalid("食物名称太长了");
        if (!NutritionUnits.IsKnown(draft.Unit))
            return IntakeSaveResult.Invalid("单位只能是克或毫升");
        if (double.IsNaN(draft.Amount) || double.IsInfinity(draft.Amount) || draft.Amount <= 0 || draft.Amount > 10000)
            return IntakeSaveResult.Invalid("分量要大于 0，并且不超过 10000");

        var food = await FindFoodAsync(name).ConfigureAwait(false);
        if (food is null)
        {
            if (draft.PerHundred is null)
            {
                var similar = await FindSimilarAsync(name, draft.Unit).ConfigureAwait(false);
                return IntakeSaveResult.NeedChoice($"列表里没有「{name}」，要新增吗？", similar);
            }

            var defined = draft.PerHundred.Value;
            var error = NutritionMath.ValidatePerHundred(defined);
            if (error is not null)
                return IntakeSaveResult.Invalid(error);
            food = await InsertFoodAsync(name, draft.Unit, defined).ConfigureAwait(false);
        }

        if (food.Unit != draft.Unit)
            return IntakeSaveResult.Invalid($"这个食物按{NutritionMath.UnitLabel(food.Unit)}记录");

        var nutrients = NutrientIo.FromFood(food).Scale(draft.Amount);
        var intake = new IntakeEntity
        {
            EpochDay = draft.EpochDay,
            FoodId = food.Id,
            FoodName = food.Name,
            Amount = draft.Amount,
            Unit = food.Unit,
        };
        NutrientIo.Apply(intake, nutrients);
        await _database.Connection.InsertAsync(intake).ConfigureAwait(false);
        return IntakeSaveResult.Saved;
    }

    public async Task<string?> SaveSunIntakeAsync(long epochDay, double minutes, double skinFraction, double seasonFactor)
    {
        var error = SunExposure.Validate(minutes, skinFraction, seasonFactor);
        if (error is not null)
            return error;

        var intake = new IntakeEntity
        {
            EpochDay = epochDay,
            FoodName = SunExposure.Name,
            Amount = minutes,
            Unit = NutritionUnits.Minute,
            VitaminD = SunExposure.VitaminDMicrograms(minutes, skinFraction, seasonFactor),
        };
        await _database.Connection.InsertAsync(intake).ConfigureAwait(false);
        return null;
    }

    public Task DeleteIntakeAsync(long intakeId) =>
        _database.Connection.DeleteAsync<IntakeEntity>(intakeId);

    private async Task<FoodEntity?> FindFoodAsync(string name)
    {
        var key = NutritionMath.NameKey(name);
        return await _database.Connection.Table<FoodEntity>()
            .Where(food => food.NameKey == key)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<FoodCandidate>> FindSimilarAsync(string name, string unit)
    {
        var foods = await _database.Connection.Table<FoodEntity>().ToListAsync().ConfigureAwait(false);
        return foods
            .Where(food => food.Unit == unit && NutritionMath.IsApproximateName(name, food.Name))
            .OrderByDescending(food => NutritionMath.SharedRunLength(name, food.Name))
            .ThenBy(food => Math.Abs(NutritionMath.NameKey(food.Name).Length - NutritionMath.NameKey(name).Length))
            .ThenBy(food => food.Name, StringComparer.Ordinal)
            .Take(NutritionMath.ApproximateLimit)
            .Select(food => new FoodCandidate(food.Name, food.Unit))
            .ToList();
    }

    private async Task<FoodEntity> InsertFoodAsync(string name, string unit, NutrientValues perHundred)
    {
        var entity = new FoodEntity
        {
            NameKey = NutritionMath.NameKey(name),
            Name = name,
            Unit = unit,
            BuiltIn = false,
        };
        NutrientIo.Apply(entity, perHundred);
        await _database.Connection.InsertAsync(entity).ConfigureAwait(false);
        return entity;
    }
}
