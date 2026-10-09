using Microsoft.Maui.Controls.Shapes;

namespace WeekPlan.App.Ui;

public partial class NutritionDetailPage : ContentView, IPanel<NutritionDetailData>
{
    private readonly AppCoordinator _coordinator;
    private readonly Dictionary<long, FoodViews> _foods = new();
    private readonly List<StatViews> _stats = new();
    private readonly List<SupplementViews> _supplements = new();

    public NutritionDetailPage(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
    }

    public bool IsOpen { get; private set; }

    public NutritionDetailData? Data { get; private set; }

    public void Open(NutritionDetailData data)
    {
        IsOpen = true;
        Apply(data);
    }

    public void Refresh(NutritionDetailData data)
    {
        if (IsOpen)
            Apply(data);
    }

    public void Close()
    {
        IsOpen = false;
        Data = null;
        _foods.Clear();
        FoodHost.Children.Clear();
        _stats.Clear();
        _supplements.Clear();
        SummaryHost.Children.Clear();
    }

    private void Apply(NutritionDetailData data)
    {
        Data = data;
        DateLabel.Text = data.DateLabel;
        PaintGoal(data.Cutting);
        EmptyFoodsLabel.IsVisible = data.ShowEmpty;
        RenderSummary(data);
        RenderFoods(data.Foods);
    }

    private void RenderSummary(NutritionDetailData data)
    {
        if (_stats.Count != data.Stats.Count || _supplements.Count != data.Supplements.Count)
        {
            SummaryHost.Children.Clear();
            _stats.Clear();
            _supplements.Clear();
            SummaryHost.Children.Add(BuildSummaryCard(data.Stats.Count));
            SummaryHost.Children.Add(BuildSupplementCard(data.Supplements.Count));
        }

        for (var index = 0; index < data.Stats.Count; index++)
            _stats[index].Apply(data.Stats[index]);
        for (var index = 0; index < data.Supplements.Count; index++)
            _supplements[index].Apply(data.Supplements[index]);
    }

    private Border BuildSummaryCard(int count)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Auto),
                new(GridLength.Star),
                new(GridLength.Star),
                new(GridLength.Star),
            },
            ColumnSpacing = 8,
            RowSpacing = 8,
        };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.Add(Header("营养素", TextAlignment.Start), 0, 0);
        grid.Add(Header("当前", TextAlignment.End), 1, 0);
        grid.Add(Header("差值", TextAlignment.End), 2, 0);
        grid.Add(Header("目标", TextAlignment.End), 3, 0);
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(1)));
        var line = new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#EEF0F4"),
            VerticalOptions = LayoutOptions.Fill,
        };
        grid.Add(line, 0, 1);
        Grid.SetColumnSpan(line, 4);

        for (var index = 0; index < count; index++)
        {
            var row = index + 2;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var name = Cell(TextAlignment.Start);
            var current = Cell(TextAlignment.End);
            var difference = Cell(TextAlignment.End);
            var target = Cell(TextAlignment.End);
            grid.Add(name, 0, row);
            grid.Add(current, 1, row);
            grid.Add(difference, 2, row);
            grid.Add(target, 3, row);
            _stats.Add(new StatViews(name, current, difference, target));
        }

        return new Border
        {
            Padding = new Thickness(14, 12),
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Shadow = SoftShadow(),
            Content = grid,
        };
    }

    private Border BuildSupplementCard(int count)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Auto),
                new(GridLength.Star),
            },
            ColumnSpacing = 12,
            RowSpacing = 8,
        };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.Add(Header("健身补剂", TextAlignment.Start), 0, 0);
        grid.Add(Header("今天", TextAlignment.End), 1, 0);
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(1)));
        var line = new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#EEF0F4"),
            VerticalOptions = LayoutOptions.Fill,
        };
        grid.Add(line, 0, 1);
        Grid.SetColumnSpan(line, 2);

        for (var index = 0; index < count; index++)
        {
            var row = index + 2;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var name = Cell(TextAlignment.Start);
            var current = Cell(TextAlignment.End);
            grid.Add(name, 0, row);
            grid.Add(current, 1, row);
            _supplements.Add(new SupplementViews(name, current));
        }

        return new Border
        {
            Padding = new Thickness(14, 12),
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Shadow = SoftShadow(),
            Content = grid,
        };
    }

    private void RenderFoods(IReadOnlyList<FoodIntakeVm> foods)
    {
        var ids = foods.Select(food => food.Id).ToHashSet();
        foreach (var id in _foods.Keys.ToList())
        {
            if (ids.Contains(id))
                continue;
            FoodHost.Children.Remove(_foods[id].Root);
            _foods.Remove(id);
        }

        for (var index = 0; index < foods.Count; index++)
        {
            var food = foods[index];
            if (!_foods.TryGetValue(food.Id, out var views))
            {
                views = BuildFood(food);
                _foods[food.Id] = views;
            }

            views.Apply(food);
            var current = FoodHost.Children.IndexOf(views.Root);
            if (current != index)
            {
                if (current >= 0)
                    FoodHost.Children.RemoveAt(current);
                FoodHost.Children.Insert(index, views.Root);
            }
        }
    }

    private FoodViews BuildFood(FoodIntakeVm food)
    {
        var title = new Label
        {
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1B1F2A"),
            VerticalOptions = LayoutOptions.Center,
        };
        var amount = new Label { FontSize = 13, TextColor = Color.FromArgb("#8E95A3") };
        var values = new List<Label>();
        var stack = new VerticalStackLayout { Spacing = 6, Children = { title, amount } };
        foreach (var line in food.Lines)
        {
            var (row, value) = NutrientLine(line.Name);
            values.Add(value);
            stack.Children.Add(row);
        }

        var delete = new Button
        {
            Text = "删除",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#8C3A32"),
            HorizontalOptions = LayoutOptions.End,
            Padding = new Thickness(0, 4),
        };
        var id = food.Id;
        delete.Clicked += (_, _) => _ = _coordinator.DeleteIntakeAsync(id);
        stack.Children.Add(delete);
        var card = new Border
        {
            Padding = new Thickness(14),
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = stack,
        };
        return new FoodViews(card, title, amount, values);
    }

    private static (Grid Row, Label Value) NutrientLine(string name)
    {
        var caption = new Label
        {
            Text = name,
            TextColor = Color.FromArgb("#8E95A3"),
            VerticalOptions = LayoutOptions.Center,
        };
        var value = new Label
        {
            TextColor = Color.FromArgb("#1B1F2A"),
            HorizontalTextAlignment = TextAlignment.End,
            VerticalOptions = LayoutOptions.Center,
        };
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(new GridLength(88)),
                new(GridLength.Star),
            },
        };
        row.Add(caption);
        row.Add(value, 1);
        return (row, value);
    }

    private static Label Header(string text, TextAlignment alignment) => new()
    {
        Text = text,
        FontSize = 13,
        TextColor = Color.FromArgb("#8E95A3"),
        HorizontalTextAlignment = alignment,
        VerticalOptions = LayoutOptions.Center,
    };

    private static Label Cell(TextAlignment alignment) => new()
    {
        FontSize = 14,
        TextColor = Color.FromArgb("#1B1F2A"),
        HorizontalTextAlignment = alignment,
        VerticalOptions = LayoutOptions.Center,
        LineBreakMode = LineBreakMode.WordWrap,
    };

    private static Shadow SoftShadow() => new()
    {
        Brush = Brush.Black,
        Offset = new Point(0, 8),
        Radius = 18,
        Opacity = 0.06f,
    };

    private void OnBackClicked(object? sender, TappedEventArgs e) => _coordinator.CloseNutrition();

    private void OnGoalHelpClicked(object? sender, EventArgs e) => _coordinator.BeginGoalHelp();

    private void OnCutClicked(object? sender, EventArgs e) => _ = _coordinator.SaveGoalAsync(NutritionGoal.Cut);

    private void OnKeepClicked(object? sender, EventArgs e) => _ = _coordinator.SaveGoalAsync(NutritionGoal.Keep);

    private void PaintGoal(bool cutting)
    {
        Highlight(CutButton, cutting);
        Highlight(KeepButton, !cutting);
    }

    private static void Highlight(Button button, bool selected)
    {
        button.BackgroundColor = selected ? Color.FromArgb("#22C55E") : Color.FromArgb("#F4F1EA");
        button.TextColor = selected ? Colors.White : Color.FromArgb("#1C1B19");
    }

    private void OnAddClicked(object? sender, EventArgs e)
    {
        if (Data is null)
            return;
        _coordinator.BeginIntake(Data.EpochDay);
    }

    private sealed class StatViews
    {
        public StatViews(Label name, Label current, Label difference, Label target)
        {
            Name = name;
            Current = current;
            Difference = difference;
            Target = target;
        }

        public Label Name { get; }
        public Label Current { get; }
        public Label Difference { get; }
        public Label Target { get; }

        public void Apply(NutrientStatVm stat)
        {
            Name.Text = stat.Name;
            Current.Text = stat.Current;
            Difference.Text = stat.Difference;
            Difference.TextColor = stat.DifferenceColor;
            Target.Text = stat.Target;
        }
    }

    private sealed class SupplementViews
    {
        public SupplementViews(Label name, Label current)
        {
            Name = name;
            Current = current;
        }

        public Label Name { get; }
        public Label Current { get; }

        public void Apply(SupplementStatVm stat)
        {
            Name.Text = stat.Name;
            Current.Text = stat.Current;
        }
    }

    private sealed class FoodViews
    {
        public FoodViews(Border root, Label title, Label amount, List<Label> lines)
        {
            Root = root;
            Title = title;
            Amount = amount;
            Lines = lines;
        }

        public Border Root { get; }
        public Label Title { get; }
        public Label Amount { get; }
        public List<Label> Lines { get; }

        public void Apply(FoodIntakeVm food)
        {
            Title.Text = food.Title;
            Amount.Text = food.AmountLabel;
            var count = Math.Min(Lines.Count, food.Lines.Count);
            for (var index = 0; index < count; index++)
                Lines[index].Text = food.Lines[index].Label;
        }
    }
}
