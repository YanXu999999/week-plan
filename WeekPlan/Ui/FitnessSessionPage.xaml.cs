using Microsoft.Maui.Controls.Shapes;
using WeekPlan;

namespace WeekPlan.App.Ui;

public partial class FitnessSessionPage : ContentView, IPanel<SessionPanelData>
{
    private static readonly int[] Presets = [30, 45, 60, 90, 120, 180];
    private readonly AppCoordinator _coordinator;

    public FitnessSessionPage(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        SetsCard.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) };
        RestCard.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) };
        SetsCard.Stroke = Color.FromArgb("#E4DED2");
        RestCard.Stroke = Color.FromArgb("#E4DED2");
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
                if (Data is null || Data.Missing)
                    return;
                _ = _coordinator.ChangeRestAsync(Data.TaskId, value);
            };
            PresetHost.Children.Add(button);
        }
    }

    public bool IsOpen { get; private set; }

    public SessionPanelData? Data { get; private set; }

    public void Open(SessionPanelData data)
    {
        IsOpen = true;
        Apply(data);
    }

    public void Refresh(SessionPanelData data)
    {
        if (IsOpen)
            Apply(data);
    }

    public void Close()
    {
        IsOpen = false;
        Data = null;
    }

    private void Apply(SessionPanelData data)
    {
        Data = data;
        MissingLabel.IsVisible = data.Missing;
        Form.IsVisible = !data.Missing;
        if (data.Missing)
        {
            MissingLabel.Text = data.Title;
            return;
        }

        PhaseLabel.Text = data.PhaseLabel;
        TitleLabel.Text = data.Title;
        ProgressLabel.Text = data.ProgressLabel;
        CountdownLabel.Text = data.CountdownLabel;
        CountdownLabel.IsVisible = data.ShowCountdown;
        StartButton.IsVisible = data.ShowStart;
        CompleteButton.IsVisible = data.ShowComplete;
        DoneLabel.IsVisible = data.ShowDone;
        DoneLabel.Text = data.DoneMessage;
        CardioHint.IsVisible = data.Cardio && !data.ShowDone;
        StrengthTools.IsVisible = data.ShowStrengthTools;
        SetsLabel.Text = data.SetsLabel;
        RestLabel.Text = data.RestLabel;
        DecreaseSetsButton.IsEnabled = data.CanDecreaseSets;
        IncreaseSetsButton.IsEnabled = data.CanIncreaseSets;
        DecreaseRestButton.IsEnabled = data.CanDecreaseRest;
        IncreaseRestButton.IsEnabled = data.CanIncreaseRest;
        NotStartedButton.BackgroundColor = data.NotStartedBackground;
        NotStartedButton.TextColor = data.NotStartedText;
        InProgressButton.BackgroundColor = data.InProgressBackground;
        InProgressButton.TextColor = data.InProgressText;
        DoneButton.BackgroundColor = data.DoneBackground;
        DoneButton.TextColor = data.DoneText;
        HighlightPresets(data.RestSeconds);
    }

    private void HighlightPresets(int restSeconds)
    {
        foreach (var child in PresetHost.Children)
        {
            if (child is not Button button)
                continue;
            var selected = button.Text == $"{restSeconds}秒";
            button.BackgroundColor = selected ? Color.FromArgb("#22C55E") : Color.FromArgb("#FFFCF7");
            button.TextColor = selected ? Colors.White : Color.FromArgb("#1C1B19");
        }
    }

    private void OnBackClicked(object? sender, TappedEventArgs e) => _coordinator.CloseSession();

    private void OnStartClicked(object? sender, EventArgs e)
    {
        if (Data is null)
            return;
        _ = _coordinator.SetStatusAsync(Data.TaskId, Data.EpochDay, PlanCodes.InProgress);
    }

    private void OnCompleteClicked(object? sender, EventArgs e)
    {
        if (Data is null)
            return;
        _ = _coordinator.CompleteSetAsync(Data.TaskId, Data.EpochDay);
    }

    private void OnNotStartedClicked(object? sender, EventArgs e) => SetStatus(PlanCodes.NotStarted);

    private void OnInProgressClicked(object? sender, EventArgs e) => SetStatus(PlanCodes.InProgress);

    private void OnDoneClicked(object? sender, EventArgs e) => SetStatus(PlanCodes.Done);

    private void SetStatus(string status)
    {
        if (Data is null || Data.Missing)
            return;
        _ = _coordinator.SetStatusAsync(Data.TaskId, Data.EpochDay, status);
    }

    private void OnDecreaseSets(object? sender, EventArgs e) => ChangeSets(-1);

    private void OnIncreaseSets(object? sender, EventArgs e) => ChangeSets(1);

    private void ChangeSets(int delta)
    {
        if (Data is null || Data.Missing)
            return;
        _ = _coordinator.ChangeSetsAsync(Data.TaskId, Data.EpochDay, Data.Sets + delta);
    }

    private void OnDecreaseRest(object? sender, EventArgs e) => ChangeRest(-PlanLimits.RestStepSeconds);

    private void OnIncreaseRest(object? sender, EventArgs e) => ChangeRest(PlanLimits.RestStepSeconds);

    private void ChangeRest(int delta)
    {
        if (Data is null || Data.Missing)
            return;
        _ = _coordinator.ChangeRestAsync(Data.TaskId, Data.RestSeconds + delta);
    }
}
