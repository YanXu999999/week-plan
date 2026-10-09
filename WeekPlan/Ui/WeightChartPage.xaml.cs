using Microsoft.Maui.Controls.Shapes;

namespace WeekPlan.App.Ui;

public partial class WeightChartPage : ContentView, IPanel<WeightChartData>
{
    private readonly AppCoordinator _coordinator;
    private readonly LineChartView _chart = new();
    private string? _appliedToday;
    private bool _saving;

    public WeightChartPage(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        ChartCard.StrokeShape = new RoundRectangle { CornerRadius = 20 };
        ChartCard.Stroke = Colors.Transparent;
        _chart.EnablePointTap();
        _chart.PointChosen += OnPoint;
        ChartHost.Children.Add(_chart);
    }

    public bool IsOpen { get; private set; }

    public WeightChartData? Data { get; private set; }

    public void Open(WeightChartData data)
    {
        IsOpen = true;
        Apply(data);
    }

    public void Refresh(WeightChartData data)
    {
        if (IsOpen)
            Apply(data);
    }

    public void Close()
    {
        IsOpen = false;
        Data = null;
        _appliedToday = null;
    }

    private void Apply(WeightChartData data)
    {
        Data = data;
        AverageLabel.Text = data.ShowEmpty ? "还没有体重" : $"最近7次平均 {data.AverageLabel}";
        EmptyLabel.IsVisible = data.ShowEmpty;
        _chart.IsVisible = !data.ShowEmpty;
        _chart.SetPoints(
            data.Points.Select(point => point.Value).ToList(),
            data.MinLabel,
            data.MaxLabel,
            data.StartLabel,
            data.EndLabel,
            Color.FromArgb("#22C55E"));
        if (_appliedToday == data.TodayWeight)
            return;
        TodayEntry.Text = data.TodayWeight;
        _appliedToday = data.TodayWeight;
    }

    private void OnBack(object? sender, TappedEventArgs e) => _coordinator.CloseWeightChart();

    private void OnPoint(int index)
    {
        if (Data is null || index < 0 || index >= Data.Points.Count)
            return;
        _coordinator.BeginEditWeight(Data.Points[index].EpochDay);
    }

    private async void OnSave(object? sender, EventArgs e)
    {
        if (!IsOpen || _saving)
            return;
        if (!UiNumbers.TryParse(TodayEntry.Text, out var kilograms))
        {
            ErrorLabel.Text = "体重要填数字";
            ErrorLabel.IsVisible = true;
            return;
        }

        _saving = true;
        SaveButton.IsEnabled = false;
        try
        {
            var error = await _coordinator.SaveTodayWeightAsync(kilograms);
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
}
