using Microsoft.Maui.Controls.Shapes;
using WeekPlan;

namespace WeekPlan.App.Ui;

public partial class TaskEditorPopup : ContentView, IPanel<EditorPanelData>
{
    private static readonly int[] Presets = [30, 45, 60, 90, 120, 180];
    private static readonly int[] DurationPresets = [10, 20, 30, 45, 60, 90];
    private readonly AppCoordinator _coordinator;
    private AppPopup.Editor? _request;
    private string _type = PlanCodes.Strength;
    private int _sets = PlanLimits.DefaultSets;
    private int _rest = PlanLimits.DefaultRestSeconds;
    private int _duration = PlanLimits.DefaultDurationMinutes;
    private string _status = PlanCodes.NotStarted;
    private bool _saving;
    private int _categoryIndex;
    private bool _categoryPinned;
    private bool _reopenPicker;
    private bool _pointerInPicker;
    private int _hideToken;
    private int _pointerToken;
    private int _paintedCategory = -2;
    private Microsoft.Maui.Controls.Window? _watchedWindow;
    private readonly HashSet<Android.Views.View> _boundScrolls = [];

    public TaskEditorPopup(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        Card.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        Card.Stroke = Color.FromArgb("#E4DED2");
        PickerPanel.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) };
        MoveScroll.HandlerChanged += (_, _) => BindPickerScroll(MoveScroll);
        CategoryScroll.HandlerChanged += (_, _) => BindPickerScroll(CategoryScroll);
        foreach (var seconds in Presets)
        {
            var value = seconds;
            var button = new Button
            {
                Text = $"{value}秒",
                Padding = new Thickness(10, 6),
                FontSize = 14,
            };
            button.Clicked += (_, _) =>
            {
                _rest = value;
                ShowNumbers();
            };
            PresetHost.Children.Add(button);
        }

        foreach (var minutes in DurationPresets)
        {
            var value = minutes;
            var button = new Button
            {
                Text = $"{value}分钟",
                Padding = new Thickness(10, 6),
                FontSize = 14,
            };
            button.Clicked += (_, _) =>
            {
                _duration = value;
                ShowNumbers();
            };
            DurationPresetHost.Children.Add(button);
        }
    }

    public bool IsOpen { get; private set; }

    public EditorPanelData? Data { get; private set; }

    public long? BoundTaskId => _request?.TaskId;

    public void Bind(AppPopup.Editor request) => _request = request;

    public bool Matches(AppPopup.Editor request) =>
        _request is not null
        && _request.TaskId == request.TaskId
        && _request.DayOfWeek == request.DayOfWeek
        && _request.EpochDay == request.EpochDay;

    public void Open(EditorPanelData data)
    {
        IsOpen = true;
        Data = data;
        HideExercisePicker();
        _categoryIndex = 0;
        _categoryPinned = false;
        _type = data.Type;
        _sets = data.Sets;
        _rest = data.RestSeconds;
        _duration = data.DurationMinutes;
        _status = data.Status;
        HeadingLabel.Text = data.Heading;
        TitleEntry.Text = data.Title;
        TypeLabel.Text = data.TypeLabel;
        TypeLabel.IsVisible = data.LockType;
        TypePicker.IsVisible = !data.LockType;
        StatusFields.IsVisible = data.ShowStatus;
        DeleteButton.IsVisible = data.ShowDelete;
        ErrorLabel.IsVisible = false;
        ShowNumbers();
    }

    public void Refresh(EditorPanelData data)
    {
        if (!IsOpen)
            Open(data);
    }

    public void Close()
    {
        IsOpen = false;
        Data = null;
        HideExercisePicker();
    }

    public void HideExercisePicker()
    {
        _hideToken++;
        _pointerInPicker = false;
        _reopenPicker = false;
        if (!PickerPanel.IsVisible && VerticalOptions.Alignment == LayoutAlignment.Center)
            return;
        PickerPanel.IsVisible = false;
        DockEditor(false);
        EditorScroll.MaximumHeightRequest = 640;
    }

    private void ShowNumbers()
    {
        FitnessFields.IsVisible = _type == PlanCodes.Strength;
        CardioFields.IsVisible = _type == PlanCodes.Cardio;
        SetsLabel.Text = $"{_sets} 组";
        RestLabel.Text = $"{_rest} 秒";
        DurationLabel.Text = $"{_duration} 分钟";
        Highlight(StrengthButton, _type == PlanCodes.Strength);
        Highlight(CardioButton, _type == PlanCodes.Cardio);
        Highlight(NotStartedButton, _status == PlanCodes.NotStarted);
        Highlight(InProgressButton, _status == PlanCodes.InProgress);
        Highlight(DoneButton, _status == PlanCodes.Done);
        foreach (var child in PresetHost.Children)
        {
            if (child is Button button)
                Highlight(button, button.Text == $"{_rest}秒");
        }

        foreach (var child in DurationPresetHost.Children)
        {
            if (child is Button button)
                Highlight(button, button.Text == $"{_duration}分钟");
        }
    }

    private static void Highlight(Button button, bool selected)
    {
        button.BackgroundColor = selected ? Color.FromArgb("#22C55E") : Color.FromArgb("#F4F1EA");
        button.TextColor = selected ? Colors.White : Color.FromArgb("#1C1B19");
    }

    private void OnTypePressed(object? sender, EventArgs e)
    {
        _reopenPicker = PickerPanel.IsVisible;
        if (_reopenPicker)
            _hideToken++;
    }

    private void OnStrengthClicked(object? sender, EventArgs e)
    {
        _type = PlanCodes.Strength;
        _categoryIndex = 0;
        _categoryPinned = false;
        ShowNumbers();
        ReopenPickerAfterTypeChange();
    }

    private void OnCardioClicked(object? sender, EventArgs e)
    {
        _type = PlanCodes.Cardio;
        _categoryIndex = 0;
        _categoryPinned = false;
        ShowNumbers();
        ReopenPickerAfterTypeChange();
    }

    private void ReopenPickerAfterTypeChange()
    {
        if (!_reopenPicker)
            return;
        _reopenPicker = false;
        ShowPicker();
        TitleEntry.Focus();
    }

    private void OnTitleFocused(object? sender, FocusEventArgs e)
    {
        _hideToken++;
        if (PickerPanel.IsVisible)
            return;
        _categoryPinned = false;
        ShowPicker();
    }

    private async void OnTitleUnfocused(object? sender, FocusEventArgs e)
    {
        var token = ++_hideToken;
        await Task.Delay(320);
        if (token != _hideToken || _pointerInPicker || TitleEntry.IsFocused || _reopenPicker)
            return;
        HideExercisePicker();
    }

    private void OnTitleTextChanged(object? sender, TextChangedEventArgs e)
    {
        ClearTitleButton.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);
        if (!PickerPanel.IsVisible)
            return;
        _categoryPinned = false;
        PaintPicker();
    }

    private void OnClearTitlePressed(object? sender, EventArgs e)
    {
        if (PickerPanel.IsVisible)
            _hideToken++;
    }

    private void OnClearTitle(object? sender, EventArgs e)
    {
        _hideToken++;
        _categoryPinned = false;
        var keepOpen = PickerPanel.IsVisible;
        TitleEntry.Text = "";
        if (!keepOpen)
            return;
        ShowPicker();
        TitleEntry.Focus();
    }

    private void BindPickerScroll(ScrollView scroll)
    {
        if (scroll.Handler?.PlatformView is not Android.Views.View native || !_boundScrolls.Add(native))
            return;
        native.Touch += (_, e) =>
        {
            var motion = e.Event;
            if (motion is null)
                return;
            var action = motion.ActionMasked;
            if (action is Android.Views.MotionEventActions.Down or Android.Views.MotionEventActions.Move)
                native.Parent?.RequestDisallowInterceptTouchEvent(true);
            if (action == Android.Views.MotionEventActions.Down)
                NotePickerTouch();
            e.Handled = false;
        };
    }

    private void NotePickerTouch()
    {
        _pointerInPicker = true;
        _hideToken++;
        var token = ++_pointerToken;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(280), () =>
        {
            if (token == _pointerToken)
                _pointerInPicker = false;
        });
        if (TitleEntry.IsFocused)
            return;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(40), () =>
        {
            if (token == _pointerToken && PickerPanel.IsVisible)
                TitleEntry.Focus();
        });
    }

    private void ShowPicker()
    {
        PickerPanel.IsVisible = true;
        DockEditor(true);
        WatchWindow();
        FitToWindow();
        PaintPicker();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(280), RevealTitle);
    }

    private void DockEditor(bool dock)
    {
        VerticalOptions = dock ? LayoutOptions.Start : LayoutOptions.Center;
        Margin = dock ? new Thickness(0, 12, 0, 0) : Thickness.Zero;
        if (Parent is Microsoft.Maui.Controls.View host)
            host.VerticalOptions = dock ? LayoutOptions.Start : LayoutOptions.Center;
    }

    private void WatchWindow()
    {
        var window = Window;
        if (window is null || ReferenceEquals(_watchedWindow, window))
            return;
        if (_watchedWindow is not null)
            _watchedWindow.SizeChanged -= OnWindowSizeChanged;
        _watchedWindow = window;
        window.SizeChanged += OnWindowSizeChanged;
    }

    private void OnWindowSizeChanged(object? sender, EventArgs e)
    {
        if (!PickerPanel.IsVisible)
            return;
        FitToWindow();
        RevealTitle();
    }

    private void FitToWindow()
    {
        var height = Window?.Height ?? 0;
        if (height < 280)
            return;
        EditorScroll.MaximumHeightRequest = height - 36;
    }

    private async void RevealTitle()
    {
        if (!PickerPanel.IsVisible || !TitleRow.IsLoaded)
            return;
        FitToWindow();
        await EditorScroll.ScrollToAsync(TitleRow, ScrollToPosition.Start, true);
    }

    private void PaintPicker()
    {
        var query = TitleEntry.Text?.Trim() ?? "";
        if (_type == PlanCodes.Cardio)
        {
            CategoryColumn.IsVisible = false;
            SetCategoryColumn(false);
            var matches = ExercisePickerCatalog.Cardio.Where(name => Matches(name, query)).ToList();
            FillMoves(matches.Count == 0 ? ExercisePickerCatalog.Cardio : matches);
            _paintedCategory = -1;
            return;
        }

        CategoryColumn.IsVisible = true;
        SetCategoryColumn(true);
        var categories = ExercisePickerCatalog.Strength;
        var anyMatch = query.Length == 0 || categories.Any(category => CategoryHasMatch(category, query));
        var effectiveQuery = anyMatch ? query : "";
        if (!_categoryPinned)
            _categoryIndex = FirstMatchingCategory(categories, effectiveQuery);
        if (_categoryIndex < 0 || _categoryIndex >= categories.Count)
            _categoryIndex = 0;

        var categoryChanged = _paintedCategory != _categoryIndex;
        _paintedCategory = _categoryIndex;
        FillCategories(categories, _categoryIndex);
        var groups = categories[_categoryIndex].Groups
            .Select(group => new ExerciseGroup(group.Name, group.Moves.Where(name => Matches(name, effectiveQuery)).ToList()))
            .Where(group => group.Moves.Count > 0)
            .ToList();
        FillGroups(groups, groups.Count == 0);
        if (categoryChanged)
            _ = MoveScroll.ScrollToAsync(0, 0, false);
    }

    private void SetCategoryColumn(bool show)
    {
        if (CategoryColumn.Parent is not Grid grid || grid.ColumnDefinitions.Count == 0)
            return;
        grid.ColumnDefinitions[0].Width = show ? GridLength.Auto : new GridLength(0);
    }

    private static int FirstMatchingCategory(IReadOnlyList<ExerciseCategory> categories, string query)
    {
        if (query.Length == 0)
            return 0;
        for (var i = 0; i < categories.Count; i++)
        {
            if (CategoryHasMatch(categories[i], query))
                return i;
        }

        return 0;
    }

    private static bool CategoryHasMatch(ExerciseCategory category, string query) =>
        category.Groups.Any(group => group.Moves.Any(name => Matches(name, query)));

    private static bool Matches(string name, string query) =>
        query.Length == 0 || name.Contains(query, StringComparison.OrdinalIgnoreCase);

    private void FillCategories(IReadOnlyList<ExerciseCategory> categories, int selected)
    {
        CategoryHost.Children.Clear();
        for (var i = 0; i < categories.Count; i++)
        {
            var index = i;
            var on = i == selected;
            var row = new Grid
            {
                Padding = new Thickness(0, 6),
                BackgroundColor = on ? Color.FromArgb("#F4FBF7") : Colors.Transparent,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(3) },
                    new ColumnDefinition { Width = GridLength.Star },
                },
            };
            row.Add(new BoxView
            {
                Color = on ? Color.FromArgb("#22C55E") : Colors.Transparent,
                WidthRequest = 3,
                InputTransparent = true,
            });
            row.Add(new Label
            {
                Text = categories[i].Name,
                FontSize = 12,
                FontAttributes = on ? FontAttributes.Bold : FontAttributes.None,
                TextColor = on ? Color.FromArgb("#1F6B4A") : Color.FromArgb("#6F6A62"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                InputTransparent = true,
            }, 1, 0);
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => SelectCategory(index);
            row.GestureRecognizers.Add(tap);
            CategoryHost.Children.Add(row);
        }
    }

    private void SelectCategory(int index)
    {
        _hideToken++;
        _categoryIndex = index;
        _categoryPinned = true;
        PaintPicker();
    }

    private void FillMoves(IReadOnlyList<string> moves)
    {
        FillGroups([new ExerciseGroup("", moves)], moves.Count == 0);
    }

    private void FillGroups(IReadOnlyList<ExerciseGroup> groups, bool empty)
    {
        MoveHost.Children.Clear();
        if (empty)
        {
            MoveHost.Children.Add(new Label
            {
                Text = "没有匹配的动作",
                FontSize = 12,
                TextColor = Color.FromArgb("#8E95A3"),
                Margin = new Thickness(8, 8, 8, 0),
            });
            return;
        }

        foreach (var group in groups)
        {
            if (group.Name.Length > 0)
            {
                MoveHost.Children.Add(new Label
                {
                    Text = group.Name,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1C1B19"),
                    Margin = new Thickness(6, 6, 4, 0),
                });
            }

            var rowCount = (group.Moves.Count + 2) / 3;
            var grid = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            for (var column = 0; column < 3; column++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            for (var row = 0; row < rowCount; row++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (var i = 0; i < group.Moves.Count; i++)
            {
                var name = group.Moves[i];
                var label = new Label
                {
                    Text = name,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#1C1B19"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap,
                    Padding = new Thickness(2, 6),
                };
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => SelectMove(name);
                label.GestureRecognizers.Add(tap);
                grid.Add(label, i % 3, i / 3);
            }

            MoveHost.Children.Add(grid);
        }
    }

    private void SelectMove(string name)
    {
        _hideToken++;
        _pointerInPicker = false;
        HideExercisePicker();
        TitleEntry.Text = name;
        TitleEntry.Unfocus();
    }

    private void OnDecreaseDuration(object? sender, EventArgs e)
    {
        _duration = Math.Max(PlanLimits.MinDurationMinutes, _duration - PlanLimits.DurationStepMinutes);
        ShowNumbers();
    }

    private void OnIncreaseDuration(object? sender, EventArgs e)
    {
        _duration = Math.Min(PlanLimits.MaxDurationMinutes, _duration + PlanLimits.DurationStepMinutes);
        ShowNumbers();
    }

    private void OnDecreaseSets(object? sender, EventArgs e)
    {
        _sets = Math.Max(PlanLimits.MinSets, _sets - 1);
        ShowNumbers();
    }

    private void OnIncreaseSets(object? sender, EventArgs e)
    {
        _sets = Math.Min(PlanLimits.MaxSets, _sets + 1);
        ShowNumbers();
    }

    private void OnDecreaseRest(object? sender, EventArgs e)
    {
        _rest = Math.Max(PlanLimits.MinRestSeconds, _rest - PlanLimits.RestStepSeconds);
        ShowNumbers();
    }

    private void OnIncreaseRest(object? sender, EventArgs e)
    {
        _rest = Math.Min(PlanLimits.MaxRestSeconds, _rest + PlanLimits.RestStepSeconds);
        ShowNumbers();
    }

    private void OnNotStarted(object? sender, EventArgs e)
    {
        _status = PlanCodes.NotStarted;
        ShowNumbers();
    }

    private void OnInProgress(object? sender, EventArgs e)
    {
        _status = PlanCodes.InProgress;
        ShowNumbers();
    }

    private void OnDone(object? sender, EventArgs e)
    {
        _status = PlanCodes.Done;
        ShowNumbers();
    }

    private void OnCancel(object? sender, EventArgs e) => _coordinator.Pop();

    private async void OnSave(object? sender, EventArgs e)
    {
        if (Data is null || _request is null || _saving)
            return;
        var title = TitleEntry.Text?.Trim() ?? "";
        if (title.Length == 0)
        {
            ErrorLabel.IsVisible = true;
            return;
        }

        _saving = true;
        try
        {
            var type = Data.LockType ? Data.Type : _type;
            var saved = await _coordinator.SaveAsync(new TaskDraft(
                Data.TaskId,
                Data.DayOfWeek,
                Data.EpochDay,
                type,
                title,
                _sets,
                _rest,
                Data.ShowStatus ? _status : null,
                _duration));
            ErrorLabel.IsVisible = !saved;
        }
        finally
        {
            _saving = false;
        }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (Data?.TaskId is long taskId)
            _coordinator.AskDelete(taskId);
    }
}
