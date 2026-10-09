using Microsoft.Maui.Controls.Shapes;
using WeekPlan;

namespace WeekPlan.App.Ui;

public partial class FoodChoicePopup : ContentView, IPanel<FoodChoicePanelData>
{
    private readonly AppCoordinator _coordinator;
    private bool _saving;

    public FoodChoicePopup(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        Card.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        Card.Stroke = Color.FromArgb("#E4DED2");
    }

    public bool IsOpen { get; private set; }

    public FoodChoicePanelData? Data { get; private set; }

    public bool Matches(AppPopup.ConfirmNewFood popup) =>
        IsOpen
        && Data?.EpochDay == popup.EpochDay
        && Data.Name == popup.Name
        && Data.Amount == popup.Amount
        && Data.Unit == popup.Unit;

    public void Open(FoodChoicePanelData data)
    {
        IsOpen = true;
        Data = data;
        MessageLabel.Text = data.Message;
        SimilarHeadingLabel.Text = data.SimilarHeading;
        EmptyLabel.Text = data.EmptyLabel;
        EmptyLabel.IsVisible = data.ShowEmpty;
        ErrorLabel.IsVisible = false;
        MatchList.Children.Clear();
        foreach (var food in data.Foods)
        {
            var name = food.Name;
            var button = new Button
            {
                Text = food.Label,
                BackgroundColor = Color.FromArgb("#F4F1EA"),
                TextColor = Color.FromArgb("#1C1B19"),
                CornerRadius = 12,
                Padding = new Thickness(12, 8),
            };
            button.Clicked += (_, _) => OnUseExisting(name);
            MatchList.Children.Add(button);
        }
    }

    public void Refresh(FoodChoicePanelData data)
    {
        if (IsOpen)
            Open(data);
    }

    public void Close()
    {
        IsOpen = false;
        Data = null;
        MatchList.Children.Clear();
    }

    private void OnCancel(object? sender, EventArgs e) => _coordinator.Pop();

    private void OnCreate(object? sender, EventArgs e) => _coordinator.BeginNewFoodDefinition();

    private async void OnUseExisting(string name)
    {
        if (Data is null || _saving)
            return;

        _saving = true;
        try
        {
            var result = await _coordinator.SaveIntakeAsync(new IntakeDraft(
                Data.EpochDay,
                name,
                Data.Amount,
                Data.Unit,
                null));
            if (result.Kind == IntakeSaveKind.Saved || result.Kind == IntakeSaveKind.NeedChoice)
                return;
            ErrorLabel.Text = result.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            _saving = false;
        }
    }
}
