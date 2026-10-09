using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;

namespace WeekPlan.App.Ui;

public partial class RootPage : ContentPage
{
    private readonly AppCoordinator _coordinator;
    private readonly WeekPage _week;
    private FitnessSessionPage? _session;
    private NutritionDetailPage? _nutrition;
    private WeightChartPage? _weight;
    private BodyChartPage? _body;
    private TaskEditorPopup? _editor;
    private ConfirmPopup? _confirm;
    private IntakePopup? _intake;
    private FoodChoicePopup? _foodChoice;
    private TargetPopup? _targets;
    private WeightPointPopup? _weightPoint;
    private GoalHelpPopup? _goalHelp;
    private int _ticking;
    private int _exitHintGeneration;

    public RootPage(AppCoordinator coordinator)
    {
        InitializeComponent();
        ExitHint.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        _coordinator = coordinator;
        _week = new WeekPage(coordinator);
        _coordinator.Ui.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(ShowCurrentScreen);
        _coordinator.Updated += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshOpenPanels);
    }

    private void OnPopupMaskTapped(object? sender, TappedEventArgs e)
    {
        _editor?.HideExercisePicker();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ShowCurrentScreen();
        if (!_week.IsOpen)
            _week.Open(_coordinator.WeekData);
        _ = _coordinator.ReloadAsync();
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(500), () =>
        {
            if (Interlocked.Exchange(ref _ticking, 1) == 1)
                return true;
            _ = TickAsync();
            return true;
        });
    }

    private async Task TickAsync()
    {
        try
        {
            await _coordinator.TickAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            Interlocked.Exchange(ref _ticking, 0);
        }
    }

    private void ShowCurrentScreen()
    {
        switch (_coordinator.Ui.Screen)
        {
            case AppScreen.Week:
                ShowWeek();
                break;
            case AppScreen.Nutrition:
                ShowNutrition();
                break;
            case AppScreen.WeightChart:
                ShowWeight();
                break;
            case AppScreen.BodyChart:
                ShowBody();
                break;
            default:
                ShowSession();
                break;
        }

        SyncPopups();
        if (_coordinator.Ui.Popups.Count > 0 || _coordinator.Ui.Screen is not AppScreen.Week)
            HideExitHint();
    }

    protected override bool OnBackButtonPressed()
    {
        switch (_coordinator.HandleBack())
        {
            case BackStep.ConfirmExit:
                ShowExitHint();
                return true;
            case BackStep.Exit:
                HideExitHint();
                return false;
            default:
                HideExitHint();
                return true;
        }
    }

    private void ShowExitHint()
    {
        var generation = ++_exitHintGeneration;
        ExitHint.IsVisible = true;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(BackRules.ExitConfirmMillis), () =>
        {
            if (generation == _exitHintGeneration)
                ExitHint.IsVisible = false;
            return false;
        });
    }

    private void HideExitHint()
    {
        _exitHintGeneration++;
        ExitHint.IsVisible = false;
    }

    private void ShowWeek()
    {
        _session?.Close();
        _session = null;
        CloseNutrition();
        CloseWeight();
        CloseBody();
        if (!ScreenHost.Children.Contains(_week))
        {
            ScreenHost.Children.Clear();
            ScreenHost.Children.Add(_week);
        }

        if (_week.IsOpen)
            _week.Refresh(_coordinator.WeekData);
        else
            _week.Open(_coordinator.WeekData);
    }

    private void ShowNutrition()
    {
        _week.Close();
        _session?.Close();
        _session = null;
        CloseWeight();
        CloseBody();
        if (_nutrition is null || !ScreenHost.Children.Contains(_nutrition))
        {
            _nutrition?.Close();
            _nutrition = new NutritionDetailPage(_coordinator);
            ScreenHost.Children.Clear();
            ScreenHost.Children.Add(_nutrition);
        }

        if (_coordinator.NutritionData is null)
            return;
        if (_nutrition.IsOpen)
            _nutrition.Refresh(_coordinator.NutritionData);
        else
            _nutrition.Open(_coordinator.NutritionData);
    }

    private void ShowSession()
    {
        if (_session is not null && ScreenHost.Children.Contains(_session))
            return;

        _week.Close();
        CloseNutrition();
        CloseWeight();
        CloseBody();
        _session?.Close();
        _session = new FitnessSessionPage(_coordinator);
        ScreenHost.Children.Clear();
        ScreenHost.Children.Add(_session);
        if (_coordinator.SessionData is not null)
            _session.Open(_coordinator.SessionData);
    }

    private void ShowWeight()
    {
        _week.Close();
        _session?.Close();
        _session = null;
        CloseNutrition();
        CloseBody();
        if (_weight is null || !ScreenHost.Children.Contains(_weight))
        {
            _weight?.Close();
            _weight = new WeightChartPage(_coordinator);
            ScreenHost.Children.Clear();
            ScreenHost.Children.Add(_weight);
        }

        if (_weight.IsOpen)
            _weight.Refresh(_coordinator.WeightData);
        else
            _weight.Open(_coordinator.WeightData);
    }

    private void ShowBody()
    {
        _week.Close();
        _session?.Close();
        _session = null;
        CloseNutrition();
        CloseWeight();
        if (_body is null || !ScreenHost.Children.Contains(_body))
        {
            _body?.Close();
            _body = new BodyChartPage(_coordinator);
            ScreenHost.Children.Clear();
            ScreenHost.Children.Add(_body);
        }

        if (_body.IsOpen)
            _body.Refresh(_coordinator.BodyData);
        else
            _body.Open(_coordinator.BodyData);
    }

    private void RefreshOpenPanels()
    {
        switch (_coordinator.Ui.Screen)
        {
            case AppScreen.Week:
                if (_week.IsOpen)
                    _week.Refresh(_coordinator.WeekData);
                else
                    _week.Open(_coordinator.WeekData);
                break;
            case AppScreen.Nutrition:
                if (_nutrition is null || _coordinator.NutritionData is null)
                    return;
                if (_nutrition.IsOpen)
                    _nutrition.Refresh(_coordinator.NutritionData);
                else
                    _nutrition.Open(_coordinator.NutritionData);
                break;
            case AppScreen.WeightChart:
                if (_weight is null)
                    return;
                if (_weight.IsOpen)
                    _weight.Refresh(_coordinator.WeightData);
                else
                    _weight.Open(_coordinator.WeightData);
                break;
            case AppScreen.BodyChart:
                if (_body is null)
                    return;
                if (_body.IsOpen)
                    _body.Refresh(_coordinator.BodyData);
                else
                    _body.Open(_coordinator.BodyData);
                break;
            default:
                if (_session is null || _coordinator.SessionData is null)
                    return;
                if (_session.IsOpen)
                    _session.Refresh(_coordinator.SessionData);
                else
                    _session.Open(_coordinator.SessionData);
                break;
        }
    }

    private void SyncPopups()
    {
        var popups = _coordinator.Ui.Popups;
        PopupLayer.IsVisible = popups.Count > 0;
        var editor = popups.OfType<AppPopup.Editor>().FirstOrDefault();
        var confirm = popups.OfType<AppPopup.ConfirmDelete>().FirstOrDefault();
        var intake = popups.OfType<AppPopup.AddIntake>().FirstOrDefault();
        var foodChoice = popups.OfType<AppPopup.ConfirmNewFood>().FirstOrDefault();
        var targets = popups.OfType<AppPopup.EditTargets>().FirstOrDefault();
        var weightPoint = popups.OfType<AppPopup.EditWeight>().FirstOrDefault();
        var goalHelp = popups.OfType<AppPopup.GoalHelp>().FirstOrDefault();

        if (editor is null)
        {
            RemoveEditor();
        }
        else if (_editor is null || !_editor.Matches(editor))
        {
            RemoveEditor();
            _editor = new TaskEditorPopup(_coordinator);
            _editor.Bind(editor);
            PopupHost.Children.Insert(0, _editor);
            _ = LoadEditorAsync(editor);
        }

        if (confirm is null)
        {
            RemoveConfirm();
        }
        else if (_confirm is null || _confirm.TaskId != confirm.TaskId)
        {
            RemoveConfirm();
            _confirm = new ConfirmPopup(_coordinator);
            PopupHost.Children.Add(_confirm);
            _confirm.Open(new ConfirmPanelData
            {
                TaskId = confirm.TaskId,
                Message = "删除这条任务？",
            });
        }

        if (intake is null)
        {
            RemoveIntake();
        }
        else if (_intake is null || !_intake.Matches(intake.EpochDay))
        {
            RemoveIntake();
            _intake = new IntakePopup(_coordinator);
            _intake.ZIndex = 1;
            PopupHost.Children.Add(_intake);
            _intake.Open(PanelFormatter.FormatIntake(intake.EpochDay));
        }

        if (foodChoice is null)
        {
            RemoveFoodChoice();
        }
        else if (_foodChoice is null || !_foodChoice.Matches(foodChoice))
        {
            RemoveFoodChoice();
            _foodChoice = new FoodChoicePopup(_coordinator);
            _foodChoice.ZIndex = 2;
            PopupHost.Children.Add(_foodChoice);
            _foodChoice.Open(PanelFormatter.FormatFoodChoice(
                foodChoice.EpochDay,
                foodChoice.Name,
                foodChoice.Amount,
                foodChoice.Unit,
                foodChoice.Message,
                foodChoice.Similar));
        }

        if (targets is null)
        {
            RemoveTargets();
        }
        else if (_targets is null)
        {
            _targets = new TargetPopup(_coordinator);
            PopupHost.Children.Add(_targets);
            _ = LoadTargetsAsync();
        }

        if (weightPoint is null)
        {
            RemoveWeightPoint();
        }
        else if (_weightPoint is null || !_weightPoint.Matches(weightPoint.EpochDay))
        {
            RemoveWeightPoint();
            _weightPoint = new WeightPointPopup(_coordinator);
            _weightPoint.Expect(weightPoint.EpochDay);
            PopupHost.Children.Add(_weightPoint);
            _ = LoadWeightPointAsync(weightPoint.EpochDay);
        }

        if (goalHelp is null)
        {
            RemoveGoalHelp();
        }
        else if (_goalHelp is null)
        {
            _goalHelp = new GoalHelpPopup(_coordinator);
            PopupHost.Children.Add(_goalHelp);
            _goalHelp.Open(PanelFormatter.FormatGoalHelp());
        }

        if (_coordinator.TakeNewFoodDefinition())
            _intake?.ShowDefinition();
    }

    private async Task LoadEditorAsync(AppPopup.Editor editor)
    {
        try
        {
            var item = await _coordinator.LoadEditorAsync(editor);
            if (_editor is null || !_editor.Matches(editor))
                return;
            if (item is null)
            {
                _coordinator.Pop();
                return;
            }

            _editor.Open(PanelFormatter.FormatEditor(item));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private async Task LoadTargetsAsync()
    {
        try
        {
            var data = await _coordinator.LoadTargetsAsync();
            if (_targets is null)
                return;
            _targets.Open(data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private async Task LoadWeightPointAsync(long epochDay)
    {
        try
        {
            var data = await _coordinator.LoadWeightPointAsync(epochDay);
            if (_weightPoint is null || !_weightPoint.Matches(epochDay))
                return;
            if (data is null)
            {
                _coordinator.Pop();
                return;
            }

            _weightPoint.Open(data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void CloseNutrition()
    {
        if (_nutrition is null)
            return;
        ScreenHost.Children.Remove(_nutrition);
        _nutrition.Close();
        _nutrition = null;
    }

    private void CloseWeight()
    {
        if (_weight is null)
            return;
        ScreenHost.Children.Remove(_weight);
        _weight.Close();
        _weight = null;
    }

    private void CloseBody()
    {
        if (_body is null)
            return;
        ScreenHost.Children.Remove(_body);
        _body.Close();
        _body = null;
    }

    private void RemoveEditor()
    {
        if (_editor is null)
            return;
        _editor.Close();
        PopupHost.Children.Remove(_editor);
        _editor = null;
    }

    private void RemoveConfirm()
    {
        if (_confirm is null)
            return;
        PopupHost.Children.Remove(_confirm);
        _confirm.Close();
        _confirm = null;
    }

    private void RemoveIntake()
    {
        if (_intake is null)
            return;
        PopupHost.Children.Remove(_intake);
        _intake.Close();
        _intake = null;
    }

    private void RemoveFoodChoice()
    {
        if (_foodChoice is null)
            return;
        PopupHost.Children.Remove(_foodChoice);
        _foodChoice.Close();
        _foodChoice = null;
    }

    private void RemoveTargets()
    {
        if (_targets is null)
            return;
        PopupHost.Children.Remove(_targets);
        _targets.Close();
        _targets = null;
    }

    private void RemoveWeightPoint()
    {
        if (_weightPoint is null)
            return;
        PopupHost.Children.Remove(_weightPoint);
        _weightPoint.Close();
        _weightPoint = null;
    }

    private void RemoveGoalHelp()
    {
        if (_goalHelp is null)
            return;
        PopupHost.Children.Remove(_goalHelp);
        _goalHelp.Close();
        _goalHelp = null;
    }
}
