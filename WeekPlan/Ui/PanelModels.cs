namespace WeekPlan.App.Ui;

public sealed class DayChipVm
{
    public int DayOfWeek { get; init; }
    public string Label { get; init; } = "";
    public bool Selected { get; init; }
    public bool Today { get; init; }
    public Color Background { get; init; }
    public Color Foreground { get; init; }
    public Color Border { get; init; }
}

public sealed class TaskRowVm
{
    public long Id { get; init; }
    public int DayOfWeek { get; init; }
    public long EpochDay { get; init; }
    public bool Fitness { get; init; }
    public string Title { get; init; } = "";
    public string TypeLabel { get; init; } = "";
    public string StatusLabel { get; init; } = "";
    public string MetaLabel { get; init; } = "";
    public bool ShowMeta { get; init; }
    public Color MetaColor { get; init; } = Colors.Gray;
    public Color ChipBackground { get; init; } = Colors.Transparent;
    public Color ChipBorder { get; init; } = Colors.Transparent;
    public Color ChipForeground { get; init; } = Colors.Black;
    public string ActionLabel { get; init; } = "";
    public Color ActionBackground { get; init; } = Colors.White;
    public Color ActionForeground { get; init; } = Colors.Black;
    public Color ActionBorder { get; init; } = Colors.Transparent;
}

public sealed class WeekPanelData
{
    public string DateLabel { get; init; } = "";
    public string SummaryLabel { get; init; } = "";
    public bool ShowSummary { get; init; }
    public string EmptyLabel { get; init; } = "这一天还没有任务";
    public bool ShowEmpty { get; init; }
    public bool IsToday { get; init; }
    public int DayOfWeek { get; init; }
    public long EpochDay { get; init; }
    public IReadOnlyList<DayChipVm> Days { get; init; } = Array.Empty<DayChipVm>();
    public IReadOnlyList<TaskRowVm> Tasks { get; init; } = Array.Empty<TaskRowVm>();
    public NutritionCardVm Nutrition { get; init; } = new();
    public string ProfileAge { get; init; } = "";
    public string ProfileHeight { get; init; } = "";
    public string SexLabel { get; init; } = "男";
    public string BasalLabel { get; init; } = "缺少年龄、身高、体重";
    public bool BasalMissing { get; init; } = true;
    public string WeightAverage { get; init; } = "还没有体重";
    public string BodyWaist { get; init; } = "还没有记录";
    public string BodyLeftArm { get; init; } = "还没有记录";
    public string BodyRightArm { get; init; } = "还没有记录";
    public string BodyChest { get; init; } = "还没有记录";
    public string BodyLeftThigh { get; init; } = "还没有记录";
    public string BodyRightThigh { get; init; } = "还没有记录";
    public bool ShowBodyReminder { get; init; }
    public IReadOnlyList<string> MineTargets { get; init; } = Array.Empty<string>();
}

public sealed class MineSnapshot
{
    public static MineSnapshot Empty { get; } = new();

    public double? WeightAverageKg { get; init; }
    public WeekPlan.BodyMeasures? LatestBody { get; init; }
    public bool ShowBodyReminder { get; init; }
}

public sealed class ChartPointVm
{
    public long EpochDay { get; init; }
    public double Value { get; init; }
    public string DateLabel { get; init; } = "";
    public string ValueLabel { get; init; } = "";
}

public sealed class WeightChartData
{
    public string AverageLabel { get; init; } = "还没有体重";
    public string TodayWeight { get; init; } = "";
    public bool ShowEmpty { get; init; } = true;
    public string MinLabel { get; init; } = "";
    public string MaxLabel { get; init; } = "";
    public string StartLabel { get; init; } = "";
    public string EndLabel { get; init; } = "";
    public IReadOnlyList<ChartPointVm> Points { get; init; } = Array.Empty<ChartPointVm>();
}

public sealed class BodySeriesVm
{
    public string Name { get; init; } = "";
    public bool ShowEmpty { get; init; } = true;
    public string MinLabel { get; init; } = "";
    public string MaxLabel { get; init; } = "";
    public string StartLabel { get; init; } = "";
    public string EndLabel { get; init; } = "";
    public Color Line { get; init; } = Colors.Transparent;
    public IReadOnlyList<ChartPointVm> Points { get; init; } = Array.Empty<ChartPointVm>();
}

public sealed class BodyChartData
{
    public string Waist { get; init; } = "";
    public string LeftArm { get; init; } = "";
    public string RightArm { get; init; } = "";
    public string Chest { get; init; } = "";
    public string LeftThigh { get; init; } = "";
    public string RightThigh { get; init; } = "";
    public IReadOnlyList<BodySeriesVm> Series { get; init; } = Array.Empty<BodySeriesVm>();
}

public sealed class WeightPointPanelData
{
    public long EpochDay { get; init; }
    public string Heading { get; init; } = "";
    public string Weight { get; init; } = "";
}

public sealed class NutritionCardVm
{
    public string CaloriesPair { get; init; } = "0 / 2050";
    public string ProteinPair { get; init; } = "0 / 120";
    public string FatPair { get; init; } = "0 / 57";
    public string CarbsPair { get; init; } = "0 / 250";
    public string FiberPair { get; init; } = "0 / 25";
    public string SodiumPair { get; init; } = "0 / 1500";
}

public sealed class NutrientStatVm
{
    public string Name { get; init; } = "";
    public string Current { get; init; } = "";
    public string Difference { get; init; } = "";
    public Color DifferenceColor { get; init; } = Color.FromArgb("#1B1F2A");
    public string Target { get; init; } = "";
    public string Percent { get; init; } = "";
}

public sealed class SupplementStatVm
{
    public string Name { get; init; } = "";
    public string Current { get; init; } = "";
}

public sealed class SupplementChoiceVm
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Detail { get; init; } = "";
}

public sealed class FoodNutrientVm
{
    public string Name { get; init; } = "";
    public string Label { get; init; } = "";
}

public sealed class FoodIntakeVm
{
    public long Id { get; init; }
    public string Title { get; init; } = "";
    public string AmountLabel { get; init; } = "";
    public IReadOnlyList<FoodNutrientVm> Lines { get; init; } = Array.Empty<FoodNutrientVm>();
}

public sealed class GoalHelpPanelData
{
    public string Body { get; init; } = "";
}

public sealed class NutritionDetailData
{
    public long EpochDay { get; init; }
    public string DateLabel { get; init; } = "";
    public bool Cutting { get; init; }
    public IReadOnlyList<NutrientStatVm> Stats { get; init; } = Array.Empty<NutrientStatVm>();
    public IReadOnlyList<SupplementStatVm> Supplements { get; init; } = Array.Empty<SupplementStatVm>();
    public bool ShowEmpty { get; init; }
    public IReadOnlyList<FoodIntakeVm> Foods { get; init; } = Array.Empty<FoodIntakeVm>();
}

public sealed class IntakePanelData
{
    public long EpochDay { get; init; }
    public string Heading { get; init; } = "记一笔摄入";
    public IReadOnlyList<SupplementChoiceVm> Vitamins { get; init; } = Array.Empty<SupplementChoiceVm>();
    public IReadOnlyList<SupplementChoiceVm> Proteins { get; init; } = Array.Empty<SupplementChoiceVm>();
    public IReadOnlyList<SupplementChoiceVm> Fitness { get; init; } = Array.Empty<SupplementChoiceVm>();
}

public sealed class SimilarFoodVm
{
    public string Name { get; init; } = "";
    public string Label { get; init; } = "";
}

public sealed class FoodChoicePanelData
{
    public long EpochDay { get; init; }
    public string Name { get; init; } = "";
    public double Amount { get; init; }
    public string Unit { get; init; } = "";
    public string Message { get; init; } = "";
    public string SimilarHeading { get; init; } = "近似食物";
    public string EmptyLabel { get; init; } = "没有近似的食物";
    public bool ShowEmpty { get; init; }
    public IReadOnlyList<SimilarFoodVm> Foods { get; init; } = Array.Empty<SimilarFoodVm>();
}

public sealed class TargetPanelData
{
    public string Calories { get; init; } = "";
    public string Protein { get; init; } = "";
    public string Fat { get; init; } = "";
    public string Carbs { get; init; } = "";
    public string Fiber { get; init; } = "";
    public string Sodium { get; init; } = "";
    public string Calcium { get; init; } = "";
    public string Iron { get; init; } = "";
    public string Zinc { get; init; } = "";
    public string Potassium { get; init; } = "";
    public string VitaminB1 { get; init; } = "";
    public string VitaminC { get; init; } = "";
    public string VitaminD { get; init; } = "";
    public string Omega3 { get; init; } = "";
}

public sealed class SessionPanelData
{
    public bool Missing { get; init; }
    public long TaskId { get; init; }
    public long EpochDay { get; init; }
    public string Title { get; init; } = "";
    public string PhaseLabel { get; init; } = "";
    public string ProgressLabel { get; init; } = "";
    public string CountdownLabel { get; init; } = "";
    public bool ShowCountdown { get; init; }
    public bool ShowStart { get; init; }
    public bool ShowComplete { get; init; }
    public bool ShowDone { get; init; }
    public bool Cardio { get; init; }
    public bool ShowStrengthTools { get; init; } = true;
    public string DoneMessage { get; init; } = "今天这组训练已经完成";
    public string SetsLabel { get; init; } = "";
    public string RestLabel { get; init; } = "";
    public int Sets { get; init; }
    public int RestSeconds { get; init; }
    public bool CanDecreaseSets { get; init; }
    public bool CanIncreaseSets { get; init; }
    public bool CanDecreaseRest { get; init; }
    public bool CanIncreaseRest { get; init; }
    public Color NotStartedBackground { get; init; }
    public Color NotStartedText { get; init; }
    public Color InProgressBackground { get; init; }
    public Color InProgressText { get; init; }
    public Color DoneBackground { get; init; }
    public Color DoneText { get; init; }

    public static SessionPanelData MissingTask(long taskId, long epochDay) => new()
    {
        Missing = true,
        TaskId = taskId,
        EpochDay = epochDay,
        Title = "这条任务已经没有了",
    };
}

public sealed class EditorPanelData
{
    public long? TaskId { get; init; }
    public int DayOfWeek { get; init; }
    public long EpochDay { get; init; }
    public bool Creating { get; init; }
    public string Heading { get; init; } = "";
    public string Title { get; init; } = "";
    public string Type { get; init; } = WeekPlan.PlanCodes.Strength;
    public bool LockType { get; init; }
    public int Sets { get; init; }
    public int RestSeconds { get; init; }
    public int DurationMinutes { get; init; } = WeekPlan.PlanLimits.DefaultDurationMinutes;
    public string Status { get; init; } = WeekPlan.PlanCodes.NotStarted;
    public bool ShowStatus { get; init; }
    public bool ShowDelete { get; init; }
    public string TypeLabel { get; init; } = "";
}

public sealed class ConfirmPanelData
{
    public long TaskId { get; init; }
    public string Message { get; init; } = "";
}
