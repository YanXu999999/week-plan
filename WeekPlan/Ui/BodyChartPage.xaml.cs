using Microsoft.Maui.Controls.Shapes;
using WeekPlan;

namespace WeekPlan.App.Ui;

public partial class BodyChartPage : ContentView, IPanel<BodyChartData>
{
    private readonly AppCoordinator _coordinator;
    private readonly Entry[] _entries;
    private readonly string?[] _applied = new string?[6];
    private readonly List<SeriesViews> _series = new();
    private bool _saving;

    public BodyChartPage(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        _entries = new[] { WaistEntry, LeftArmEntry, RightArmEntry, ChestEntry, LeftThighEntry, RightThighEntry };
    }

    public bool IsOpen { get; private set; }

    public BodyChartData? Data { get; private set; }

    public void Open(BodyChartData data)
    {
        IsOpen = true;
        Apply(data);
    }

    public void Refresh(BodyChartData data)
    {
        if (IsOpen)
            Apply(data);
    }

    public void Close()
    {
        IsOpen = false;
        Data = null;
        Array.Clear(_applied);
        _series.Clear();
        ChartHost.Children.Clear();
    }

    private void Apply(BodyChartData data)
    {
        Data = data;
        var values = new[] { data.Waist, data.LeftArm, data.RightArm, data.Chest, data.LeftThigh, data.RightThigh };
        for (var index = 0; index < values.Length; index++)
        {
            if (_applied[index] == values[index])
                continue;
            _entries[index].Text = values[index];
            _applied[index] = values[index];
        }

        if (_series.Count != data.Series.Count)
        {
            ChartHost.Children.Clear();
            _series.Clear();
            foreach (var _ in data.Series)
            {
                var views = SeriesViews.Create();
                _series.Add(views);
                ChartHost.Children.Add(views.Root);
            }
        }

        for (var index = 0; index < data.Series.Count; index++)
            _series[index].Apply(data.Series[index]);
    }

    private void OnBack(object? sender, TappedEventArgs e) => _coordinator.CloseBodyChart();

    private async void OnSave(object? sender, EventArgs e)
    {
        if (!IsOpen || _saving)
            return;
        var names = new[] { "腰围", "左臂围", "右臂围", "胸围", "左腿围", "右腿围" };
        var numbers = new double[names.Length];
        for (var index = 0; index < names.Length; index++)
        {
            if (UiNumbers.TryParse(_entries[index].Text, out numbers[index]))
                continue;
            ErrorLabel.Text = $"{names[index]}要填数字";
            ErrorLabel.IsVisible = true;
            return;
        }

        _saving = true;
        SaveButton.IsEnabled = false;
        try
        {
            var error = await _coordinator.SaveTodayBodyAsync(new BodyMeasureDraft(
                numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]));
            if (error is null)
            {
                ErrorLabel.IsVisible = false;
                return;
            }

            ErrorLabel.Text = error;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            _saving = false;
            SaveButton.IsEnabled = true;
        }
    }

    private sealed class SeriesViews
    {
        private SeriesViews(Border root, Label title, Label empty, LineChartView chart)
        {
            Root = root;
            Title = title;
            Empty = empty;
            Chart = chart;
        }

        public Border Root { get; }
        public Label Title { get; }
        public Label Empty { get; }
        public LineChartView Chart { get; }

        public static SeriesViews Create()
        {
            var title = new Label
            {
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1B1F2A"),
            };
            var empty = new Label
            {
                Text = "还没有记录",
                FontSize = 14,
                TextColor = Color.FromArgb("#8E95A3"),
                HorizontalTextAlignment = TextAlignment.Center,
                IsVisible = false,
            };
            var chart = new LineChartView { HeightRequest = 160 };
            var body = new Grid
            {
                RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) },
                RowSpacing = 4,
            };
            body.Add(title);
            body.Add(chart, 0, 1);
            body.Add(empty, 0, 1);
            var root = new Border
            {
                Padding = new Thickness(12, 12, 12, 4),
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Content = body,
            };
            return new SeriesViews(root, title, empty, chart);
        }

        public void Apply(BodySeriesVm series)
        {
            Title.Text = series.Name;
            Empty.IsVisible = series.ShowEmpty;
            Chart.IsVisible = !series.ShowEmpty;
            Chart.SetPoints(
                series.Points.Select(point => point.Value).ToList(),
                series.MinLabel,
                series.MaxLabel,
                series.StartLabel,
                series.EndLabel,
                series.Line);
        }
    }
}
