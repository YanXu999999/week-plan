using Microsoft.Maui.Controls.Shapes;

namespace WeekPlan.App.Ui;

public partial class GoalHelpPopup : ContentView, IPanel<GoalHelpPanelData>
{
    private readonly AppCoordinator _coordinator;

    public GoalHelpPopup(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        Card.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        Card.Stroke = Color.FromArgb("#E4DED2");
    }

    public bool IsOpen { get; private set; }

    public GoalHelpPanelData? Data { get; private set; }

    public void Open(GoalHelpPanelData data)
    {
        IsOpen = true;
        Data = data;
        BodyLabel.Text = data.Body;
    }

    public void Refresh(GoalHelpPanelData data)
    {
        if (IsOpen)
            Open(data);
    }

    public void Close()
    {
        IsOpen = false;
        Data = null;
    }

    private void OnClose(object? sender, EventArgs e) => _coordinator.Pop();
}
