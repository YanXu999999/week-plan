using WeekPlan;

namespace WeekPlan.App.Ui;

public sealed class AppCoordinator
{
    private readonly WeekPlanService _service;
    private readonly NutritionService _nutrition;
    private readonly ProfileService _profile;
    private readonly BodyMetricsService _body;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DayNutrition _selectedNutrition;
    private BodyProfile _bodyProfile = BodyProfile.Empty;
    private double? _weightAverageKg;
    private bool _defineNewFood;
    private long _exitArmedUntil;

    public AppCoordinator(WeekPlanService service, NutritionService nutrition, ProfileService profile, BodyMetricsService body)
    {
        _service = service;
        _nutrition = nutrition;
        _profile = profile;
        _body = body;
        Ui = new UiManager();
        var today = DateOnly.FromDateTime(DateTime.Today);
        SelectedDay = WeekCalendar.PlanDay(today);
        var date = WeekCalendar.DateForPlanDay(SelectedDay, today);
        _selectedNutrition = DayNutrition.Empty(WeekCalendar.DayKey(date));
        WeekData = PanelFormatter.FormatWeek(
            SelectedDay,
            today,
            date,
            Array.Empty<TaskItem>(),
            AppClock.NowMillis(),
            _selectedNutrition,
            BodyProfile.Empty,
            MineSnapshot.Empty);
        WeightData = PanelFormatter.FormatWeightChart(Array.Empty<WeightEntry>(), today);
        BodyData = PanelFormatter.FormatBodyChart(Array.Empty<BodyMeasures>(), today);
    }

    public UiManager Ui { get; }

    public int SelectedDay { get; private set; }

    public WeekPanelData WeekData { get; private set; }

    public SessionPanelData? SessionData { get; private set; }

    public NutritionDetailData? NutritionData { get; private set; }

    public WeightChartData WeightData { get; private set; }

    public BodyChartData BodyData { get; private set; }

    public event EventHandler? Updated;

    public Task SelectDayAsync(int dayOfWeek)
    {
        SelectedDay = dayOfWeek;
        return ReloadAsync();
    }

    public void OpenSession(long taskId, long epochDay)
    {
        Ui.OpenScreen(new AppScreen.Session(taskId, epochDay));
        _ = ReloadAsync();
    }

    public void CloseSession()
    {
        Ui.CloseScreen();
        _ = ReloadAsync();
    }

    public void OpenNutrition(long epochDay)
    {
        if (_selectedNutrition.EpochDay == epochDay)
            NutritionData = PanelFormatter.FormatNutrition(_selectedNutrition, _bodyProfile.Goal, Basal());
        Ui.OpenScreen(new AppScreen.Nutrition(epochDay));
        _ = ReloadAsync();
    }

    public void CloseNutrition()
    {
        Ui.CloseScreen();
        _ = ReloadAsync();
    }

    public void OpenWeightChart()
    {
        Ui.OpenScreen(new AppScreen.WeightChart());
        _ = ReloadAsync();
    }

    public void CloseWeightChart()
    {
        Ui.CloseScreen();
        _ = ReloadAsync();
    }

    public void OpenBodyChart()
    {
        Ui.OpenScreen(new AppScreen.BodyChart());
        _ = ReloadAsync();
    }

    public void CloseBodyChart()
    {
        Ui.CloseScreen();
        _ = ReloadAsync();
    }

    public void BeginEditWeight(long epochDay) => Ui.Push(new AppPopup.EditWeight(epochDay));

    public void BeginIntake(long epochDay) => Ui.Push(new AppPopup.AddIntake(epochDay));

    public void BeginTargets() => Ui.Push(new AppPopup.EditTargets());

    public void BeginGoalHelp()
    {
        if (Ui.Popups.OfType<AppPopup.GoalHelp>().Any())
            return;
        Ui.Push(new AppPopup.GoalHelp());
    }

    public void Create(int dayOfWeek, long epochDay) =>
        Ui.Push(new AppPopup.Editor(null, dayOfWeek, epochDay));

    public void Edit(long taskId, int dayOfWeek, long epochDay) =>
        Ui.Push(new AppPopup.Editor(taskId, dayOfWeek, epochDay));

    public void Pop() => Ui.Pop();

    public BackStep HandleBack()
    {
        var atHome = Ui.Popups.Count == 0 && Ui.Screen is AppScreen.Week;
        var now = AppClock.NowMillis();
        var step = BackRules.Decide(atHome, now, _exitArmedUntil);
        if (step == BackStep.ReturnHome)
        {
            _exitArmedUntil = 0;
            Ui.ReturnHome();
            _ = ReloadAsync();
            return step;
        }

        _exitArmedUntil = step == BackStep.ConfirmExit ? BackRules.ArmExit(now) : 0;
        return step;
    }

    public void AskDelete(long taskId) => Ui.Push(new AppPopup.ConfirmDelete(taskId));

    public Task<EditorItem?> LoadEditorAsync(AppPopup.Editor editor) =>
        _service.GetEditorAsync(editor.TaskId, editor.DayOfWeek, editor.EpochDay);

    public async Task<TargetPanelData> LoadTargetsAsync()
    {
        var targets = await _nutrition.GetTargetsAsync();
        return PanelFormatter.FormatTargets(targets);
    }

    public async Task<bool> SaveAsync(TaskDraft draft)
    {
        var saved = await _service.SaveAsync(draft);
        if (!saved)
            return false;
        Ui.Pop();
        await ReloadAsync();
        return true;
    }

    public async Task ConfirmDeleteAsync(long taskId)
    {
        await _service.DeleteAsync(taskId);
        Ui.DismissAfterDelete(taskId);
        await ReloadAsync();
    }

    public async Task<IntakeSaveResult> SaveIntakeAsync(IntakeDraft draft)
    {
        var result = await _nutrition.SaveIntakeAsync(draft);
        if (result.Kind == IntakeSaveKind.NeedChoice)
        {
            if (Ui.Popups.Count == 0 || Ui.Popups[^1] is not AppPopup.ConfirmNewFood)
            {
                Ui.Push(new AppPopup.ConfirmNewFood(
                    draft.EpochDay,
                    NutritionMath.NormalizeName(draft.Name),
                    draft.Amount,
                    draft.Unit,
                    result.Message,
                    result.Similar));
            }

            return result;
        }

        if (result.Kind != IntakeSaveKind.Saved)
            return result;
        CloseIntakePopups();
        await ReloadAsync();
        return result;
    }

    public async Task<string?> SaveSupplementAsync(long epochDay, string id, double servings)
    {
        var error = await _nutrition.SaveSupplementAsync(epochDay, id, servings);
        if (error is not null)
            return error;
        CloseIntakePopups();
        await ReloadAsync();
        return null;
    }

    public async Task<string?> SaveSunIntakeAsync(long epochDay, double minutes, double skinFraction, double seasonFactor)
    {
        var error = await _nutrition.SaveSunIntakeAsync(epochDay, minutes, skinFraction, seasonFactor);
        if (error is not null)
            return error;
        CloseIntakePopups();
        await ReloadAsync();
        return null;
    }

    public void BeginNewFoodDefinition()
    {
        if (Ui.Popups.Count == 0 || Ui.Popups[^1] is not AppPopup.ConfirmNewFood)
            return;
        _defineNewFood = true;
        Ui.Pop();
    }

    public bool TakeNewFoodDefinition()
    {
        if (!_defineNewFood)
            return false;
        _defineNewFood = false;
        return true;
    }

    private void CloseIntakePopups()
    {
        while (Ui.Popups.Count > 0 && Ui.Popups[^1] is AppPopup.ConfirmNewFood or AppPopup.AddIntake)
            Ui.Pop();
    }

    public async Task ToggleSexAsync()
    {
        await _profile.ToggleSexAsync();
        await ReloadAsync();
    }

    public async Task SaveGoalAsync(string goal)
    {
        await _profile.SaveGoalAsync(goal);
        await ReloadAsync();
    }

    private double? Basal() => MetabolismRules.Basal(
        _bodyProfile.Age,
        _bodyProfile.HeightCm,
        _weightAverageKg,
        _bodyProfile.Sex);

    public async Task<string?> SaveAgeAsync(int age)
    {
        var error = await _profile.SaveAgeAsync(age);
        if (error is not null)
            return error;
        await ReloadAsync();
        return null;
    }

    public async Task<string?> SaveHeightAsync(double heightCm)
    {
        var error = await _profile.SaveHeightAsync(heightCm);
        if (error is not null)
            return error;
        await ReloadAsync();
        return null;
    }

    public async Task<string?> SaveTodayWeightAsync(double kilograms)
    {
        var today = WeekCalendar.DayKey(DateOnly.FromDateTime(DateTime.Today));
        var error = await _body.SaveWeightAsync(today, kilograms, today);
        if (error is not null)
            return error;
        await ReloadAsync();
        return null;
    }

    public async Task<string?> UpdateWeightAsync(long epochDay, double kilograms)
    {
        var today = WeekCalendar.DayKey(DateOnly.FromDateTime(DateTime.Today));
        var error = await _body.SaveWeightAsync(epochDay, kilograms, today);
        if (error is not null)
            return error;
        Ui.Pop();
        await ReloadAsync();
        return null;
    }

    public async Task DeleteWeightAsync(long epochDay)
    {
        await _body.DeleteWeightAsync(epochDay);
        Ui.Pop();
        await ReloadAsync();
    }

    public async Task<string?> SaveTodayBodyAsync(BodyMeasureDraft draft)
    {
        var today = WeekCalendar.DayKey(DateOnly.FromDateTime(DateTime.Today));
        var error = await _body.SaveMeasuresAsync(today, draft);
        if (error is not null)
            return error;
        await ReloadAsync();
        return null;
    }

    public async Task<WeightPointPanelData?> LoadWeightPointAsync(long epochDay)
    {
        var entry = await _body.FindWeightAsync(epochDay);
        return entry is null ? null : PanelFormatter.FormatWeightPoint(entry);
    }

    public async Task<string?> SaveTargetsAsync(TargetDraft draft)
    {
        var error = await _nutrition.SaveTargetsAsync(draft);
        if (error is not null)
            return error;
        Ui.Pop();
        await ReloadAsync();
        return null;
    }

    public async Task DeleteIntakeAsync(long intakeId)
    {
        await _nutrition.DeleteIntakeAsync(intakeId);
        await ReloadAsync();
    }

    public async Task SetStatusAsync(long taskId, long epochDay, string status)
    {
        await _service.SetFitnessStatusAsync(taskId, epochDay, status, AppClock.NowMillis());
        await ReloadAsync();
    }

    public async Task CompleteSetAsync(long taskId, long epochDay)
    {
        await _service.CompleteSetAsync(taskId, epochDay, AppClock.NowMillis());
        await ReloadAsync();
    }

    public async Task ChangeSetsAsync(long taskId, long epochDay, int sets)
    {
        await _service.ChangeSetsAsync(taskId, epochDay, sets);
        await ReloadAsync();
    }

    public async Task ChangeRestAsync(long taskId, int restSeconds)
    {
        await _service.ChangeRestAsync(taskId, restSeconds);
        await ReloadAsync();
    }

    public Task TickAsync() => ReloadAsync();

    public async Task ReloadAsync()
    {
        await _gate.WaitAsync();
        WeekPanelData? week = null;
        SessionPanelData? session = null;
        NutritionDetailData? nutrition = null;
        WeightChartData? weightChart = null;
        BodyChartData? bodyChart = null;
        DayNutrition? selectedNutrition = null;
        var hasSession = false;
        var showNutrition = false;
        try
        {
            var now = AppClock.NowMillis();
            var today = DateOnly.FromDateTime(DateTime.Today);
            var date = WeekCalendar.DateForPlanDay(SelectedDay, today);
            var key = WeekCalendar.DayKey(date);
            await _service.SyncRestsAsync(key, now);
            if (Ui.Screen is AppScreen.Session open && open.EpochDay != key)
                await _service.SyncRestsAsync(open.EpochDay, now);

            var items = await _service.GetDayAsync(SelectedDay, key);
            selectedNutrition = await _nutrition.GetDayAsync(key);
            var profile = await _profile.GetAsync();
            var weights = await _body.GetWeightsAsync();
            var measures = await _body.GetMeasuresAsync();
            var latestBody = measures.Count == 0 ? null : measures[^1];
            var showReminder = await _body.ShowReminderAsync(date, today, latestBody);
            var weightAverage = WeightRules.AverageOfLatest(weights);
            _bodyProfile = profile;
            _weightAverageKg = weightAverage;
            var mine = new MineSnapshot
            {
                WeightAverageKg = weightAverage,
                LatestBody = latestBody,
                ShowBodyReminder = showReminder,
            };
            week = PanelFormatter.FormatWeek(SelectedDay, today, date, items, now, selectedNutrition, profile, mine);
            weightChart = PanelFormatter.FormatWeightChart(weights, today);
            bodyChart = PanelFormatter.FormatBodyChart(measures, today);
            if (Ui.Screen is AppScreen.Session openSession)
            {
                hasSession = true;
                var raw = await _service.GetSessionAsync(openSession.TaskId, openSession.EpochDay);
                session = PanelFormatter.FormatSession(raw, openSession.TaskId, openSession.EpochDay, now);
            }

            if (Ui.Screen is AppScreen.Nutrition openNutrition)
            {
                showNutrition = true;
                var source = openNutrition.EpochDay == key
                    ? selectedNutrition
                    : await _nutrition.GetDayAsync(openNutrition.EpochDay);
                nutrition = PanelFormatter.FormatNutrition(source, profile.Goal, MetabolismRules.Basal(
                    profile.Age,
                    profile.HeightCm,
                    weightAverage,
                    profile.Sex));
            }
        }
        finally
        {
            _gate.Release();
        }

        if (week is null || selectedNutrition is null)
            return;

        _selectedNutrition = selectedNutrition;
        WeekData = week;
        if (weightChart is not null)
            WeightData = weightChart;
        if (bodyChart is not null)
            BodyData = bodyChart;
        SessionData = hasSession ? session : null;
        NutritionData = showNutrition ? nutrition : null;
        Updated?.Invoke(this, EventArgs.Empty);
    }
}
