using Microsoft.Maui.Controls.Shapes;
using WeekPlan;

namespace WeekPlan.App.Ui;

public partial class IntakePopup : ContentView, IPanel<IntakePanelData>
{
    private readonly AppCoordinator _coordinator;
    private readonly Dictionary<string, Button> _supplementButtons = new();
    private string _unit = NutritionUnits.Gram;
    private double _skinFraction = 0.25;
    private double _seasonFactor = SunExposure.Summer;
    private string? _selectedSupplementId;
    private bool _saving;
    private bool _fillingAmount;
    private bool _amountEdited;

    public IntakePopup(AppCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;
        Card.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        Card.Stroke = Color.FromArgb("#E4DED2");
        StyleSection(FoodCard);
        StyleSection(SupplementCard);
        StyleSection(SunCard);
        ShowUnit();
        ShowSkin();
        ShowSeason();
        ShowSunResult();
    }

    public bool IsOpen { get; private set; }

    public IntakePanelData? Data { get; private set; }

    public bool Matches(long epochDay) => IsOpen && Data?.EpochDay == epochDay;

    public void Open(IntakePanelData data)
    {
        IsOpen = true;
        Data = data;
        HeadingLabel.Text = data.Heading;
        _amountEdited = false;
        _fillingAmount = true;
        NameEntry.Text = "";
        AmountEntry.Text = "";
        _fillingAmount = false;
        PortionHint.IsVisible = false;
        CaloriesEntry.Text = "";
        ProteinEntry.Text = "";
        FatEntry.Text = "";
        CarbsEntry.Text = "";
        FiberEntry.Text = "";
        SodiumEntry.Text = "";
        CalciumEntry.Text = "";
        IronEntry.Text = "";
        ZincEntry.Text = "";
        PotassiumEntry.Text = "";
        VitaminB1Entry.Text = "";
        VitaminCEntry.Text = "";
        VitaminDEntry.Text = "";
        Omega3Entry.Text = "";
        _unit = NutritionUnits.Gram;
        _skinFraction = 0.25;
        _seasonFactor = SunExposure.Summer;
        MinutesEntry.Text = "";
        ServingsEntry.Text = "1";
        _selectedSupplementId = null;
        SupplementDetailLabel.Text = "先点一个补剂。";
        DefinitionFields.IsVisible = false;
        FoodErrorLabel.IsVisible = false;
        SupplementErrorLabel.IsVisible = false;
        SunErrorLabel.IsVisible = false;
        ShowUnit();
        ShowSkin();
        ShowSeason();
        ShowSunResult();
        FillSupplements(data);
    }

    public void Refresh(IntakePanelData data)
    {
        if (!IsOpen)
            Open(data);
    }

    public void Close()
    {
        IsOpen = false;
        Data = null;
    }

    public void ShowDefinition()
    {
        DefinitionFields.IsVisible = true;
        FoodErrorLabel.IsVisible = false;
    }

    private void FillSupplements(IntakePanelData data)
    {
        _supplementButtons.Clear();
        FillChoices(VitaminHost, data.Vitamins);
        FillChoices(ProteinHost, data.Proteins);
        FillChoices(FitnessHost, data.Fitness);
    }

    private void FillChoices(VerticalStackLayout host, IReadOnlyList<SupplementChoiceVm> items)
    {
        host.Children.Clear();
        for (var index = 0; index < items.Count; index += 2)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new(GridLength.Star),
                    new(GridLength.Star),
                },
                ColumnSpacing = 8,
            };
            grid.Add(MakeSupplementButton(items[index]));
            if (index + 1 < items.Count)
                grid.Add(MakeSupplementButton(items[index + 1]), 1);
            host.Children.Add(grid);
        }
    }

    private Button MakeSupplementButton(SupplementChoiceVm item)
    {
        var button = new Button
        {
            Text = item.Name,
            FontSize = 14,
            Padding = new Thickness(8, 8),
        };
        button.Clicked += (_, _) => SelectSupplement(item);
        _supplementButtons[item.Id] = button;
        Highlight(button, false);
        return button;
    }

    private void SelectSupplement(SupplementChoiceVm item)
    {
        _selectedSupplementId = item.Id;
        SupplementDetailLabel.Text = item.Detail;
        SupplementErrorLabel.IsVisible = false;
        foreach (var pair in _supplementButtons)
            Highlight(pair.Value, pair.Key == item.Id);
    }

    private void OnFaceHandsClicked(object? sender, EventArgs e) => SelectSkin(0.10);

    private void OnForearmsClicked(object? sender, EventArgs e) => SelectSkin(0.25);

    private void OnShortsClicked(object? sender, EventArgs e) => SelectSkin(0.40);

    private void OnMostSkinClicked(object? sender, EventArgs e) => SelectSkin(0.60);

    private void OnSpringClicked(object? sender, EventArgs e) => SelectSeason(SunExposure.Spring);

    private void OnSummerClicked(object? sender, EventArgs e) => SelectSeason(SunExposure.Summer);

    private void OnAutumnClicked(object? sender, EventArgs e) => SelectSeason(SunExposure.Autumn);

    private void OnWinterClicked(object? sender, EventArgs e) => SelectSeason(SunExposure.Winter);

    private void SelectSkin(double fraction)
    {
        _skinFraction = fraction;
        ShowSkin();
        ShowSunResult();
    }

    private void SelectSeason(double factor)
    {
        _seasonFactor = factor;
        ShowSeason();
        ShowSunResult();
    }

    private void OnSunInputChanged(object? sender, TextChangedEventArgs e) => ShowSunResult();

    private void ShowSkin()
    {
        Highlight(FaceHandsButton, _skinFraction == 0.10);
        Highlight(ForearmsButton, _skinFraction == 0.25);
        Highlight(ShortsButton, _skinFraction == 0.40);
        Highlight(MostSkinButton, _skinFraction == 0.60);
    }

    private void ShowSeason()
    {
        Highlight(SpringButton, _seasonFactor == SunExposure.Spring);
        Highlight(SummerButton, _seasonFactor == SunExposure.Summer);
        Highlight(AutumnButton, _seasonFactor == SunExposure.Autumn);
        Highlight(WinterButton, _seasonFactor == SunExposure.Winter);
    }

    private void ShowSunResult()
    {
        if (!UiNumbers.TryParse(MinutesEntry.Text, out var minutes))
        {
            SunResultLabel.Text = "填好分钟后，这里显示维生素D。";
            return;
        }

        var vitaminD = SunExposure.VitaminDMicrograms(minutes, _skinFraction, _seasonFactor);
        SunResultLabel.Text = $"约 {NutritionMath.FormatNumber(vitaminD)} 微克维生素D";
    }

    private void OnNameChanged(object? sender, TextChangedEventArgs e) => ApplyPortion();

    private void OnAmountChanged(object? sender, TextChangedEventArgs e)
    {
        if (_fillingAmount)
            return;
        if (string.IsNullOrWhiteSpace(AmountEntry.Text))
        {
            _amountEdited = false;
            ApplyPortion();
            return;
        }

        _amountEdited = true;
    }

    private void ApplyPortion()
    {
        var food = NutritionCatalog.Find(NameEntry.Text);
        if (food is null)
        {
            PortionHint.IsVisible = false;
            if (_unit != NutritionUnits.Gram)
            {
                _unit = NutritionUnits.Gram;
                ShowUnit();
            }

            return;
        }

        if (_unit != food.Unit)
        {
            _unit = food.Unit;
            ShowUnit();
        }

        if (!_amountEdited)
        {
            _fillingAmount = true;
            AmountEntry.Text = NutritionMath.FormatNumber(food.Portion.Amount);
            _fillingAmount = false;
        }

        PortionHint.Text = $"{food.Portion.Describe(food.Unit)}，可改";
        PortionHint.IsVisible = true;
    }

    private void ShowUnit()
    {
        PerHundredLabel.Text = NutritionMath.PerHundredLabel(_unit);
    }

    private static void StyleSection(Border card)
    {
        card.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) };
        card.Stroke = Color.FromArgb("#E4DED2");
        card.BackgroundColor = Colors.White;
    }

    private static void Highlight(Button button, bool selected)
    {
        button.BackgroundColor = selected ? Color.FromArgb("#22C55E") : Color.FromArgb("#F4F1EA");
        button.TextColor = selected ? Colors.White : Color.FromArgb("#1C1B19");
    }

    private void OnCancel(object? sender, EventArgs e) => _coordinator.Pop();

    private async void OnSaveFood(object? sender, EventArgs e)
    {
        if (Data is null || _saving)
            return;

        if (!UiNumbers.TryParse(AmountEntry.Text, out var amount))
        {
            ShowFoodError("分量要填数字");
            return;
        }

        NutrientValues? perHundred = null;
        if (DefinitionFields.IsVisible)
        {
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
                ShowFoodError("请补全每 100 单位的营养");
                return;
            }

            perHundred = new NutrientValues(
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
                omega3);
        }

        _saving = true;
        try
        {
            var result = await _coordinator.SaveIntakeAsync(new IntakeDraft(
                Data.EpochDay,
                NameEntry.Text ?? "",
                amount,
                _unit,
                perHundred));
            if (result.Kind == IntakeSaveKind.Saved || result.Kind == IntakeSaveKind.NeedChoice)
            {
                FoodErrorLabel.IsVisible = false;
                return;
            }

            if (result.Kind == IntakeSaveKind.NeedDefinition)
                DefinitionFields.IsVisible = true;
            ShowFoodError(result.Message);
        }
        finally
        {
            _saving = false;
        }
    }

    private async void OnSaveSupplement(object? sender, EventArgs e)
    {
        if (Data is null || _saving)
            return;
        if (_selectedSupplementId is null)
        {
            ShowSupplementError("先点一个补剂");
            return;
        }

        if (!UiNumbers.TryParse(ServingsEntry.Text, out var servings))
        {
            ShowSupplementError("份数要填数字");
            return;
        }

        _saving = true;
        try
        {
            var error = await _coordinator.SaveSupplementAsync(Data.EpochDay, _selectedSupplementId, servings);
            if (error is null)
            {
                SupplementErrorLabel.IsVisible = false;
                return;
            }

            ShowSupplementError(error);
        }
        finally
        {
            _saving = false;
        }
    }

    private async void OnSaveSun(object? sender, EventArgs e)
    {
        if (Data is null || _saving)
            return;
        if (!UiNumbers.TryParse(MinutesEntry.Text, out var minutes))
        {
            ShowSunError("晒的时间要填数字");
            return;
        }

        _saving = true;
        try
        {
            var error = await _coordinator.SaveSunIntakeAsync(Data.EpochDay, minutes, _skinFraction, _seasonFactor);
            if (error is null)
            {
                SunErrorLabel.IsVisible = false;
                return;
            }

            ShowSunError(error);
        }
        finally
        {
            _saving = false;
        }
    }

    private void ShowFoodError(string message)
    {
        FoodErrorLabel.Text = message;
        FoodErrorLabel.IsVisible = true;
    }

    private void ShowSupplementError(string message)
    {
        SupplementErrorLabel.Text = message;
        SupplementErrorLabel.IsVisible = true;
    }

    private void ShowSunError(string message)
    {
        SunErrorLabel.Text = message;
        SunErrorLabel.IsVisible = true;
    }
}
