using Microsoft.Maui.Controls.Shapes;
using WeekPlan;
using ShapePath = Microsoft.Maui.Controls.Shapes.Path;

namespace WeekPlan.App.Ui;

public partial class WeekPage : ContentView, IPanel<WeekPanelData>
{
    private const int HomeTab = 0;
    private const int PlanTab = 1;
    private const int MineTab = 2;

    private readonly AppCoordinator _coordinator;
    private readonly Dictionary<long, RowViews> _rows = new();
    private readonly List<Label> _targetLabels = new();
    private readonly ShapePath[] _tabIcons = new ShapePath[3];
    private readonly Label[] _tabLabels = new Label[3];
    private int _dayBarKey = -1;
    private int _tab = HomeTab;
    private string? _appliedAge;
    private string? _appliedHeight;
    private bool _savingProfile;
    private bool _profileOutsideTap;

    public WeekPage(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        RefreshButton.StrokeShape = new RoundRectangle { CornerRadius = 22 };
        MineCard.StrokeShape = new RoundRectangle { CornerRadius = 20 };
        MineCard.Stroke = Colors.Transparent;
        MineCard.Shadow = SoftShadow();
        AddButton.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#FF7A18")),
            Offset = new Point(0, 8),
            Radius = 16,
            Opacity = 0.28f,
        };
        var refreshTap = new TapGestureRecognizer();
        refreshTap.Tapped += (_, _) => OnRefresh();
        RefreshButton.GestureRecognizers.Add(refreshTap);
        NutritionCard.StrokeShape = new RoundRectangle { CornerRadius = 22 };
        NutritionCard.Stroke = Colors.Transparent;
        NutritionCard.Shadow = SoftShadow();
        TargetCard.StrokeShape = new RoundRectangle { CornerRadius = 20 };
        TargetCard.Stroke = Colors.Transparent;
        TargetCard.Shadow = SoftShadow();
        ProfileCard.StrokeShape = new RoundRectangle { CornerRadius = 20 };
        ProfileCard.Stroke = Colors.Transparent;
        ProfileCard.Shadow = SoftShadow();
        BodyCard.StrokeShape = new RoundRectangle { CornerRadius = 20 };
        BodyCard.Stroke = Colors.Transparent;
        BodyCard.Shadow = SoftShadow();
        BodyReminderCard.StrokeShape = new RoundRectangle { CornerRadius = 20 };
        var reminderTap = new TapGestureRecognizer();
        reminderTap.Tapped += (_, _) => OnBodyReminder();
        BodyReminderCard.GestureRecognizers.Add(reminderTap);
        foreach (var cell in new[] { CalorieCell, ProteinCell, FatCell, CarbsCell, FiberCell, SodiumCell })
        {
            cell.StrokeShape = new RoundRectangle { CornerRadius = 16 };
            cell.Stroke = Colors.Transparent;
        }

        AddIntakeButton.StrokeShape = new RoundRectangle { CornerRadius = 22 };
        NutritionDetailButton.StrokeShape = new RoundRectangle { CornerRadius = 22 };
        NutritionDetailButton.Content = InfoIcon();
        var addIntakeTap = new TapGestureRecognizer();
        addIntakeTap.Tapped += (_, _) => OnAddIntake();
        AddIntakeButton.GestureRecognizers.Add(addIntakeTap);
        var detailTap = new TapGestureRecognizer();
        detailTap.Tapped += (_, _) => OnNutritionDetail();
        NutritionDetailButton.GestureRecognizers.Add(detailTap);
        BuildTabs();
    }

    public bool IsOpen { get; private set; }

    public WeekPanelData? Data { get; private set; }

    public void Open(WeekPanelData data)
    {
        IsOpen = true;
        Apply(data);
    }

    public void Refresh(WeekPanelData data)
    {
        if (IsOpen)
            Apply(data);
    }

    public void Close()
    {
        IsOpen = false;
        ReleaseProfileOutsideTap(force: true);
        Data = null;
        _dayBarKey = -1;
        _appliedAge = null;
        _appliedHeight = null;
        _rows.Clear();
        DayBar.Children.Clear();
        TaskHost.Children.Clear();
    }

    private void Apply(WeekPanelData data)
    {
        Data = data;
        TitleLabel.Text = TitleFor(data);
        RefreshButton.IsVisible = _tab == HomeTab;
        DayBar.IsVisible = _tab != MineTab;
        AddButton.IsVisible = _tab != MineTab;
        MineHost.IsVisible = _tab == MineTab;
        TaskScroll.IsVisible = _tab != MineTab;
        NutritionCard.IsVisible = _tab == HomeTab;
        TaskHost.IsVisible = _tab != MineTab;
        var tasks = VisibleTasks(data);
        EmptyLabel.Text = EmptyText();
        EmptyLabel.IsVisible = _tab != MineTab && _tab != HomeTab && tasks.Count == 0;
        HomeEmptyLabel.IsVisible = _tab == HomeTab && tasks.Count == 0;
        PaintNutrition(data);
        PaintTargets(data);
        PaintProfile(data);
        PaintBody(data);
        BodyReminderCard.IsVisible = _tab == HomeTab && data.ShowBodyReminder;
        PaintTabs();
        if (_tab != MineTab)
        {
            RenderDays(data.Days);
            RenderTasks(tasks);
        }
    }

    private string TitleFor(WeekPanelData data) => _tab switch
    {
        PlanTab => "本周计划",
        MineTab => "我的",
        _ => data.IsToday ? "今日任务" : data.DateLabel,
    };

    private string EmptyText() => _tab switch
    {
        PlanTab => "这一天还没有健身任务",
        _ => "这一天还没有任务",
    };

    private static IReadOnlyList<TaskRowVm> VisibleTasks(WeekPanelData data) =>
        data.Tasks.Where(task => task.Fitness).ToList();

    private void RenderDays(IReadOnlyList<DayChipVm> days)
    {
        var today = days.Any(day => day.Today) ? 1 : 0;
        var selected = days.FirstOrDefault(day => day.Selected)?.DayOfWeek ?? 0;
        var key = selected * 10 + today;
        if (key == _dayBarKey && DayBar.Children.Count == days.Count)
            return;

        _dayBarKey = key;
        DayBar.Children.Clear();
        for (var index = 0; index < days.Count; index++)
        {
            var day = days[index];
            var planDay = day.DayOfWeek;
            var label = new Label
            {
                Text = day.Label,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = day.Foreground,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };
            var circle = new Border
            {
                WidthRequest = 40,
                HeightRequest = 40,
                StrokeThickness = 1.5,
                BackgroundColor = day.Background,
                Stroke = day.Border,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Content = label,
                HorizontalOptions = LayoutOptions.Center,
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => _ = _coordinator.SelectDayAsync(planDay);
            circle.GestureRecognizers.Add(tap);
            DayBar.Add(circle, index);
        }
    }

    private void RenderTasks(IReadOnlyList<TaskRowVm> tasks)
    {
        var ids = tasks.Select(task => task.Id).ToHashSet();
        foreach (var id in _rows.Keys.ToList())
        {
            if (ids.Contains(id))
                continue;
            TaskHost.Children.Remove(_rows[id].Root);
            _rows.Remove(id);
        }

        for (var index = 0; index < tasks.Count; index++)
        {
            var task = tasks[index];
            if (!_rows.TryGetValue(task.Id, out var views))
            {
                views = BuildRow();
                _rows[task.Id] = views;
            }

            views.Apply(task);
            var current = TaskHost.Children.IndexOf(views.Root);
            if (current != index)
            {
                if (current >= 0)
                    TaskHost.Children.RemoveAt(current);
                TaskHost.Children.Insert(index, views.Root);
            }
        }
    }

    private RowViews BuildRow()
    {
        var title = new Label
        {
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1B1F2A"),
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalOptions = LayoutOptions.Center,
        };
        var chipLabel = new Label { FontSize = 12, VerticalOptions = LayoutOptions.Center };
        var chip = new Border
        {
            Padding = new Thickness(8, 2),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = chipLabel,
            VerticalOptions = LayoutOptions.Center,
        };
        var titleRow = new HorizontalStackLayout { Spacing = 8, Children = { title, chip } };
        var meta = new Label { FontSize = 13 };
        var text = new VerticalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children = { titleRow, meta },
        };
        var action = new Button
        {
            HeightRequest = 40,
            CornerRadius = 20,
            Padding = new Thickness(14, 0),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            MinimumWidthRequest = 76,
        };
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Star),
                new(GridLength.Auto),
            },
            ColumnSpacing = 10,
        };
        grid.Add(text);
        grid.Add(action, 1);
        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Padding = new Thickness(16, 16, 14, 16),
            Content = grid,
            Shadow = SoftShadow(),
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => OnEdit(card);
        text.GestureRecognizers.Add(tap);
        action.Clicked += (_, _) => OnAction(card);
        return new RowViews(card, title, chip, chipLabel, meta, action);
    }

    private static Shadow SoftShadow() => new()
    {
        Brush = Brush.Black,
        Offset = new Point(0, 8),
        Radius = 18,
        Opacity = 0.06f,
    };

    private void OnAction(BindableObject source)
    {
        if (source.BindingContext is not TaskRowVm row)
            return;
        if (row.Fitness)
            _coordinator.OpenSession(row.Id, row.EpochDay);
    }

    private void OnEdit(BindableObject source)
    {
        if (source.BindingContext is not TaskRowVm row)
            return;
        _coordinator.Edit(row.Id, row.DayOfWeek, row.EpochDay);
    }

    private void OnAddClicked(object? sender, EventArgs e)
    {
        if (Data is null)
            return;
        _coordinator.Create(Data.DayOfWeek, Data.EpochDay);
    }

    private void PaintNutrition(WeekPanelData data)
    {
        NutritionDateLabel.Text = data.DateLabel;
        CaloriePairLabel.Text = data.Nutrition.CaloriesPair;
        ProteinPairLabel.Text = data.Nutrition.ProteinPair;
        FatPairLabel.Text = data.Nutrition.FatPair;
        CarbsPairLabel.Text = data.Nutrition.CarbsPair;
        FiberPairLabel.Text = data.Nutrition.FiberPair;
        SodiumPairLabel.Text = data.Nutrition.SodiumPair;
    }

    private void PaintProfile(WeekPanelData data)
    {
        if (_appliedAge != data.ProfileAge)
        {
            AgeEntry.Text = data.ProfileAge;
            _appliedAge = data.ProfileAge;
            ProfileErrorLabel.IsVisible = false;
        }

        if (_appliedHeight != data.ProfileHeight)
        {
            HeightEntry.Text = data.ProfileHeight;
            _appliedHeight = data.ProfileHeight;
            ProfileErrorLabel.IsVisible = false;
        }

        PaintSex(data.SexLabel != "女");
        BasalLabel.Text = data.BasalLabel;
        BasalLabel.TextColor = data.BasalMissing ? Color.FromArgb("#8C3A32") : Color.FromArgb("#1B1F2A");
    }

    private void OnSexClicked(object? sender, EventArgs e) => _ = _coordinator.ToggleSexAsync();

    private void PaintSex(bool male)
    {
        MaleButton.TextColor = Color.FromArgb(male ? "#1F6B4A" : "#8E95A3");
        MaleButton.FontAttributes = male ? FontAttributes.Bold : FontAttributes.None;
        MaleButton.BackgroundColor = male ? Color.FromArgb("#F4FBF7") : Colors.Transparent;
        FemaleButton.TextColor = Color.FromArgb(male ? "#8E95A3" : "#1F6B4A");
        FemaleButton.FontAttributes = male ? FontAttributes.None : FontAttributes.Bold;
        FemaleButton.BackgroundColor = male ? Colors.Transparent : Color.FromArgb("#F4FBF7");
    }

    private void PaintBody(WeekPanelData data)
    {
        WeightAverageLabel.Text = data.WeightAverage;
        BodyWaistLabel.Text = data.BodyWaist;
        BodyLeftArmLabel.Text = data.BodyLeftArm;
        BodyRightArmLabel.Text = data.BodyRightArm;
        BodyChestLabel.Text = data.BodyChest;
        BodyLeftThighLabel.Text = data.BodyLeftThigh;
        BodyRightThighLabel.Text = data.BodyRightThigh;
    }

    private void OnProfileEntryFocused(object? sender, FocusEventArgs e)
    {
        if (_profileOutsideTap)
            return;
        _profileOutsideTap = true;
        MainActivity.OutsideTap += OnProfileOutsideTap;
    }

    private void OnProfileOutsideTap(Android.Views.MotionEvent motion)
    {
        if (!AgeEntry.IsFocused && !HeightEntry.IsFocused)
        {
            ReleaseProfileOutsideTap(force: true);
            return;
        }

        if (HitEntry(AgeEntry, motion) || HitEntry(HeightEntry, motion))
            return;

        DismissProfileEntry(AgeEntry.IsFocused ? AgeEntry : HeightEntry);
    }

    private void ReleaseProfileOutsideTap(bool force = false)
    {
        if (!_profileOutsideTap)
            return;
        if (!force && (AgeEntry.IsFocused || HeightEntry.IsFocused))
            return;
        _profileOutsideTap = false;
        MainActivity.OutsideTap -= OnProfileOutsideTap;
    }

    private static bool HitEntry(Entry entry, Android.Views.MotionEvent motion)
    {
        if (entry.Handler?.PlatformView is not Android.Views.View view)
            return false;
        var location = new int[2];
        view.GetLocationOnScreen(location);
        return motion.RawX >= location[0]
            && motion.RawX <= location[0] + view.Width
            && motion.RawY >= location[1]
            && motion.RawY <= location[1] + view.Height;
    }

    private static void DismissProfileEntry(Entry entry)
    {
        var view = entry.Handler?.PlatformView as Android.Views.View;
        entry.Unfocus();
        if (view?.Context?.GetSystemService(Android.Content.Context.InputMethodService) is not Android.Views.InputMethods.InputMethodManager input)
            return;
        input.HideSoftInputFromWindow(view.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
    }

    private async void OnAgeUnfocused(object? sender, FocusEventArgs e)
    {
        ReleaseProfileOutsideTap();
        if (!IsOpen || _savingProfile)
            return;
        var text = AgeEntry.Text?.Trim() ?? "";
        if (text == (_appliedAge ?? ""))
        {
            ProfileErrorLabel.IsVisible = false;
            return;
        }

        if (!UiNumbers.TryParseInt(text, out var age))
        {
            ProfileErrorLabel.Text = "年龄要填整数";
            ProfileErrorLabel.IsVisible = true;
            return;
        }

        _savingProfile = true;
        try
        {
            var error = await _coordinator.SaveAgeAsync(age);
            if (error is null)
            {
                ProfileErrorLabel.IsVisible = false;
                return;
            }

            ProfileErrorLabel.Text = error;
            ProfileErrorLabel.IsVisible = true;
        }
        finally
        {
            _savingProfile = false;
        }
    }

    private async void OnHeightUnfocused(object? sender, FocusEventArgs e)
    {
        ReleaseProfileOutsideTap();
        if (!IsOpen || _savingProfile)
            return;
        var text = HeightEntry.Text?.Trim() ?? "";
        if (text == (_appliedHeight ?? ""))
        {
            ProfileErrorLabel.IsVisible = false;
            return;
        }

        if (!UiNumbers.TryParse(text, out var height))
        {
            ProfileErrorLabel.Text = "身高要填数字";
            ProfileErrorLabel.IsVisible = true;
            return;
        }

        _savingProfile = true;
        try
        {
            var error = await _coordinator.SaveHeightAsync(height);
            if (error is null)
            {
                ProfileErrorLabel.IsVisible = false;
                return;
            }

            ProfileErrorLabel.Text = error;
            ProfileErrorLabel.IsVisible = true;
        }
        finally
        {
            _savingProfile = false;
        }
    }

    private void OnOpenWeightChart(object? sender, EventArgs e) => _coordinator.OpenWeightChart();

    private void OnOpenBodyChart(object? sender, EventArgs e) => _coordinator.OpenBodyChart();

    private void OnBodyReminder()
    {
        _tab = MineTab;
        _coordinator.OpenBodyChart();
    }

    private void PaintTargets(WeekPanelData data)
    {
        if (_targetLabels.Count != data.MineTargets.Count)
        {
            MineTargetHost.Children.Clear();
            _targetLabels.Clear();
            foreach (var _ in data.MineTargets)
            {
                var label = new Label { TextColor = Color.FromArgb("#1B1F2A"), FontSize = 16 };
                _targetLabels.Add(label);
                MineTargetHost.Children.Add(label);
            }
        }

        for (var index = 0; index < data.MineTargets.Count; index++)
            _targetLabels[index].Text = data.MineTargets[index];
    }

    private void OnAddIntake()
    {
        if (Data is null)
            return;
        _coordinator.BeginIntake(Data.EpochDay);
    }

    private void OnNutritionDetail()
    {
        if (Data is null)
            return;
        _coordinator.OpenNutrition(Data.EpochDay);
    }

    private void OnEditTargets(object? sender, EventArgs e) => _coordinator.BeginTargets();

    private static ShapePath InfoIcon() => new()
    {
        Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString(
            "M12,2.5 A9.5,9.5 0 1 0 12.01,2.5 M12,11 V16 M12,8 H12.01")!,
        Stroke = Color.FromArgb("#1B1F2A"),
        StrokeThickness = 1.6,
        StrokeLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        Aspect = Stretch.Uniform,
        WidthRequest = 20,
        HeightRequest = 20,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center,
    };

    private void OnRefresh()
    {
        var today = Data?.Days.FirstOrDefault(day => day.Today);
        if (today is null)
            return;
        _ = _coordinator.SelectDayAsync(today.DayOfWeek);
    }

    private void BuildTabs()
    {
        var names = new[] { "首页", "计划", "我的" };
        var glyphs = new[]
        {
            "M4,11 L12,4 L20,11 V20 H15 V14 H9 V20 H4 Z",
            "M5,5 H19 V20 H5 Z M5,9 H19 M9,5 V9",
            "M12,13 A4,4 0 1 0 12,5 A4,4 0 1 0 12,13 M5,20 C5,16 8,15 12,15 C16,15 19,16 19,20",
        };
        for (var index = 0; index < names.Length; index++)
        {
            var tab = index;
            var icon = new ShapePath
            {
                Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString(glyphs[index])!,
                StrokeThickness = 1.6,
                StrokeLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Aspect = Stretch.Uniform,
                WidthRequest = 22,
                HeightRequest = 22,
                HorizontalOptions = LayoutOptions.Center,
            };
            var label = new Label
            {
                Text = names[index],
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
            };
            var stack = new VerticalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true,
                CascadeInputTransparent = true,
                Children = { icon, label },
            };
            var hit = new Grid
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                BackgroundColor = Colors.Transparent,
                Children = { stack },
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                _tab = tab;
                if (Data is not null)
                    Apply(Data);
            };
            hit.GestureRecognizers.Add(tap);
            TabBar.Add(hit, index);
            _tabIcons[index] = icon;
            _tabLabels[index] = label;
        }

        PaintTabs();
    }

    private void PaintTabs()
    {
        for (var index = 0; index < _tabIcons.Length; index++)
        {
            var color = index == _tab ? Color.FromArgb("#1B1F2A") : Color.FromArgb("#B0B6C0");
            _tabIcons[index].Stroke = color;
            _tabLabels[index].TextColor = color;
            _tabLabels[index].FontAttributes = index == _tab ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    private sealed class RowViews
    {
        public RowViews(Border root, Label title, Border chip, Label chipLabel, Label meta, Button action)
        {
            Root = root;
            Title = title;
            Chip = chip;
            ChipLabel = chipLabel;
            Meta = meta;
            Action = action;
        }

        public Border Root { get; }
        public Label Title { get; }
        public Border Chip { get; }
        public Label ChipLabel { get; }
        public Label Meta { get; }
        public Button Action { get; }

        public void Apply(TaskRowVm task)
        {
            Root.BindingContext = task;
            Title.Text = task.Title;
            ChipLabel.Text = task.StatusLabel;
            ChipLabel.TextColor = task.ChipForeground;
            Chip.BackgroundColor = task.ChipBackground;
            Chip.Stroke = task.ChipBorder;
            Meta.Text = task.MetaLabel;
            Meta.TextColor = task.MetaColor;
            Meta.IsVisible = task.ShowMeta;
            Action.Text = task.ActionLabel;
            Action.BackgroundColor = task.ActionBackground;
            Action.TextColor = task.ActionForeground;
            Action.BorderColor = task.ActionBorder;
            Action.BorderWidth = task.ActionBorder == Colors.Transparent ? 0 : 1;
        }
    }
}
