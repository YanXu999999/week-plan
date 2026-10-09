using Microsoft.Maui.Controls.Shapes;
using WeekPlan;

namespace WeekPlan.App.Ui;

public partial class TargetPopup : ContentView, IPanel<TargetPanelData>
{
    private readonly AppCoordinator _coordinator;
    private bool _saving;

    public TargetPopup(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        Card.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        Card.Stroke = Color.FromArgb("#E4DED2");
    }

    public bool IsOpen { get; private set; }

    public TargetPanelData? Data { get; private set; }

    public void Open(TargetPanelData data)
    {
        IsOpen = true;
        Data = data;
        CaloriesEntry.Text = data.Calories;
        ProteinEntry.Text = data.Protein;
        FatEntry.Text = data.Fat;
        CarbsEntry.Text = data.Carbs;
        FiberEntry.Text = data.Fiber;
        SodiumEntry.Text = data.Sodium;
        CalciumEntry.Text = data.Calcium;
        IronEntry.Text = data.Iron;
        ZincEntry.Text = data.Zinc;
        PotassiumEntry.Text = data.Potassium;
        VitaminB1Entry.Text = data.VitaminB1;
        VitaminCEntry.Text = data.VitaminC;
        VitaminDEntry.Text = data.VitaminD;
        Omega3Entry.Text = data.Omega3;
        ErrorLabel.IsVisible = false;
    }

    public void Refresh(TargetPanelData data)
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

    private async void OnSave(object? sender, EventArgs e)
    {
        if (!IsOpen || _saving)
            return;
        if (!UiNumbers.TryParse(CaloriesEntry.Text, out var calories)
            || !UiNumbers.TryParse(ProteinEntry.Text, out var protein)
            || !UiNumbers.TryParse(FatEntry.Text, out var fat)
            || !UiNumbers.TryParse(CarbsEntry.Text, out var carbs)
            || !UiNumbers.TryParse(FiberEntry.Text, out var fiber)
            || !UiNumbers.TryParse(SodiumEntry.Text, out var sodium)
            || !UiNumbers.TryParse(CalciumEntry.Text, out var calcium)
            || !UiNumbers.TryParse(IronEntry.Text, out var iron)
            || !UiNumbers.TryParse(ZincEntry.Text, out var zinc)
            || !UiNumbers.TryParse(PotassiumEntry.Text, out var potassium)
            || !UiNumbers.TryParse(VitaminB1Entry.Text, out var vitaminB1)
            || !UiNumbers.TryParse(VitaminCEntry.Text, out var vitaminC)
            || !UiNumbers.TryParse(VitaminDEntry.Text, out var vitaminD)
            || !UiNumbers.TryParse(Omega3Entry.Text, out var omega3))
        {
            ErrorLabel.Text = "目标都要填数字";
            ErrorLabel.IsVisible = true;
            return;
        }

        _saving = true;
        try
        {
            var error = await _coordinator.SaveTargetsAsync(new TargetDraft(
                calories,
                protein,
                fat,
                carbs,
                fiber,
                sodium,
                calcium,
                iron,
                zinc,
                potassium,
                vitaminB1,
                vitaminC,
                vitaminD,
                omega3));
            if (error is null)
                return;
            ErrorLabel.Text = error;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            _saving = false;
        }
    }
}
