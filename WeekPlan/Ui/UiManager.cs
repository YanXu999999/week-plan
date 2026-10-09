using WeekPlan;

namespace WeekPlan.App.Ui;

public abstract record AppScreen
{
    public sealed record Week : AppScreen;
    public sealed record Session(long TaskId, long EpochDay) : AppScreen;
    public sealed record Nutrition(long EpochDay) : AppScreen;
    public sealed record WeightChart : AppScreen;
    public sealed record BodyChart : AppScreen;
}

public abstract record AppPopup
{
    public sealed record Editor(long? TaskId, int DayOfWeek, long EpochDay) : AppPopup;
    public sealed record ConfirmDelete(long TaskId) : AppPopup;
    public sealed record AddIntake(long EpochDay) : AppPopup;
    public sealed record ConfirmNewFood(
        long EpochDay,
        string Name,
        double Amount,
        string Unit,
        string Message,
        IReadOnlyList<FoodCandidate> Similar) : AppPopup;
    public sealed record EditTargets : AppPopup;
    public sealed record EditWeight(long EpochDay) : AppPopup;
    public sealed record GoalHelp : AppPopup;
}

public sealed class UiManager
{
    private readonly List<AppPopup> _popups = new();

    public AppScreen Screen { get; private set; } = new AppScreen.Week();

    public IReadOnlyList<AppPopup> Popups => _popups;

    public event EventHandler? Changed;

    public void OpenScreen(AppScreen screen)
    {
        Screen = screen;
        _popups.Clear();
        Notify();
    }

    public void CloseScreen()
    {
        Screen = new AppScreen.Week();
        Notify();
    }

    public void ReturnHome()
    {
        if (_popups.Count == 0 && Screen is AppScreen.Week)
            return;
        _popups.Clear();
        Screen = new AppScreen.Week();
        Notify();
    }

    public void Push(AppPopup popup)
    {
        _popups.Add(popup);
        Notify();
    }

    public void Pop()
    {
        if (_popups.Count == 0)
            return;
        _popups.RemoveAt(_popups.Count - 1);
        Notify();
    }

    public void DismissAfterDelete(long taskId)
    {
        _popups.Clear();
        if (Screen is AppScreen.Session session && session.TaskId == taskId)
            Screen = new AppScreen.Week();
        Notify();
    }

    private void Notify() => Changed?.Invoke(this, EventArgs.Empty);
}
