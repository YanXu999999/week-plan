using Microsoft.Maui.Controls.Shapes;

namespace WeekPlan.App.Ui;

public partial class ConfirmPopup : ContentView, IPanel<ConfirmPanelData>
{
    private readonly AppCoordinator _coordinator;

    public ConfirmPopup(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        Card.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        Card.Stroke = Color.FromArgb("#E4DED2");
    }

    public bool IsOpen { get; private set; }

    public ConfirmPanelData? Data { get; private set; }

    public long TaskId => Data?.TaskId ?? 0;

    public void Open(ConfirmPanelData data)
    {
        IsOpen = true;
        Data = data;
        MessageLabel.Text = data.Message;
    }

    public void Refresh(ConfirmPanelData data)
    {
        if (IsOpen)
            Open(data);
    }

    public void Close()
    {
        IsOpen = false;
        Data = null;
    }

    private void OnCancel(object? sender, EventArgs e) => _coordinator.Pop();

    private void OnDelete(object? sender, EventArgs e)
    {
        if (Data is null)
            return;
        _ = _coordinator.ConfirmDeleteAsync(Data.TaskId);
    }
}
