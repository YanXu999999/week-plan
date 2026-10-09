using Microsoft.Maui.Controls.Shapes;

namespace WeekPlan.App.Ui;

public partial class WeightPointPopup : ContentView, IPanel<WeightPointPanelData>
{
    private readonly AppCoordinator _coordinator;
    private long _epoch;
    private bool _saving;

    public WeightPointPopup(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        Card.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        Card.Stroke = Color.FromArgb("#E4DED2");
    }

    public bool IsOpen { get; private set; }

    public WeightPointPanelData? Data { get; private set; }

    public long EpochDay => _epoch;

    public void Expect(long epochDay) => _epoch = epochDay;

    public bool Matches(long epochDay) => _epoch == epochDay;

    public void Open(WeightPointPanelData data)
    {
        IsOpen = true;
        Data = data;
        HeadingLabel.Text = data.Heading;
        WeightEntry.Text = data.Weight;
        ErrorLabel.IsVisible = false;
    }

    public void Refresh(WeightPointPanelData data)
    {
        if (!IsOpen || Data?.EpochDay != data.EpochDay)
            Open(data);
    }

    public void Close()
    {
        IsOpen = false;
        Data = null;
    }

    private void OnCancel(object? sender, EventArgs e) => _coordinator.Pop();

    private async void OnSave(object? sender, EventArgs e)
    {
        if (!IsOpen || Data is null || _saving)
            return;
        if (!UiNumbers.TryParse(WeightEntry.Text, out var kilograms))
        {
            ErrorLabel.Text = "体重要填数字";
            ErrorLabel.IsVisible = true;
            return;
        }

        _saving = true;
        SaveButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;
        try
        {
            var error = await _coordinator.UpdateWeightAsync(Data.EpochDay, kilograms);
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
            DeleteButton.IsEnabled = true;
        }
    }

    private async void OnDelete(object? sender, EventArgs e)
    {
        if (!IsOpen || Data is null || _saving)
            return;
        _saving = true;
        SaveButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;
        try
        {
            await _coordinator.DeleteWeightAsync(Data.EpochDay);
        }
        finally
        {
            _saving = false;
            SaveButton.IsEnabled = true;
            DeleteButton.IsEnabled = true;
        }
    }
}
