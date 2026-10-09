using WeekPlan;

namespace WeekPlan.App.Ui;

public static class PanelFormatter
{
    private static readonly Color Ink = Color.FromArgb("#1B1F2A");
    private static readonly Color Accent = Color.FromArgb("#22C55E");
    private static readonly Color Card = Colors.White;
    private static readonly Color Line = Color.FromArgb("#E6E8EE");
    private static readonly Color Orange = Color.FromArgb("#F5A04A");
    private static readonly Color Dark = Color.FromArgb("#2C3340");

    public static WeekPanelData FormatWeek(
        int selectedDay,
        DateOnly today,
        DateOnly date,
        IReadOnlyList<TaskItem> items,
        long now,
        DayNutrition nutrition,
        BodyProfile profile,
        MineSnapshot mine)
    {
        var chips = new List<DayChipVm>(7);
        for (var day = 1; day <= 7; day++)
        {
            var selected = day == selectedDay;
            var isToday = WeekCalendar.DateForPlanDay(day, today) == today;
            chips.Add(new DayChipVm
            {
                DayOfWeek = day,
                Label = day.ToString(),
                Selected = selected,
                Today = isToday,
                Background = selected ? Accent : Card,
                Foreground = selected ? Colors.White : Ink,
                Border = selected || isToday ? Accent : Line,
            });
        }

        var done = items.Count(item => item.Status == PlanCodes.Done);
        return new WeekPanelData
        {
            DateLabel = date == today ? $"{date.Month}月{date.Day}日 · 今天" : $"{date.Month}月{date.Day}日",
            SummaryLabel = $"已完成 {done} / {items.Count}",
            ShowSummary = items.Count > 0,
            EmptyLabel = "这一天还没有任务",
            ShowEmpty = items.Count == 0,
            IsToday = date == today,
            DayOfWeek = selectedDay,
            EpochDay = WeekCalendar.DayKey(date),
            Days = chips,
            Tasks = items.Select(item => ToRow(item, selectedDay, WeekCalendar.DayKey(date), now)).ToList(),
            Nutrition = FormatCard(nutrition),
            ProfileAge = profile.Age?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
            ProfileHeight = profile.HeightCm is double height ? NutritionMath.FormatNumber(height) : "",
            SexLabel = BodySex.Label(profile.Sex),
            BasalLabel = MetabolismRules.Describe(profile.Age, profile.HeightCm, mine.WeightAverageKg, profile.Sex),
            BasalMissing = MetabolismRules.Missing(profile.Age, profile.HeightCm, mine.WeightAverageKg).Count > 0,
            WeightAverage = FormatWeightAverage(mine.WeightAverageKg),
            BodyWaist = FormatMeasure(mine.LatestBody?.WaistCm),
            BodyLeftArm = FormatMeasure(mine.LatestBody?.LeftArmCm),
            BodyRightArm = FormatMeasure(mine.LatestBody?.RightArmCm),
            BodyChest = FormatMeasure(mine.LatestBody?.ChestCm),
            BodyLeftThigh = FormatMeasure(mine.LatestBody?.LeftThighCm),
            BodyRightThigh = FormatMeasure(mine.LatestBody?.RightThighCm),
            ShowBodyReminder = mine.ShowBodyReminder,
            MineTargets = NutritionFact.All
                .Select(fact => $"{fact.Name} {NutritionMath.FormatNumber(fact.Target(nutrition.Targets))} {fact.Unit}")
                .ToList(),
        };
    }

    public static GoalHelpPanelData FormatGoalHelp() => new()
    {
        Body = """
            减脂：
            热量看的是比目标少了多少千卡，还要先看有没有吃到基础代谢以下。
            吃得比基础代谢还低，是红的。
            比目标多吃了，是红的。
            比目标少了超过 1000 千卡，是红的。
            比目标少了 200 到 500 千卡，是绿的，这是想要的缺口。
            少得不到 200 千卡，或者少了 500 到 1000 千卡，是黄的。

            脂肪和碳水看的是吃到目标的几成。
            不到一半，是黄的。
            从一半到刚好吃满，是绿的。
            超了，但不超过一成，是黄的。
            超了一成以上，是红的。

            增肌、维持：
            热量、脂肪、碳水都是以接近目标值为判断依据。
            不到目标的九成，是红的。
            从九成到超一成，是绿的。
            超了一成到三成，是黄的。
            超了三成以上，是红的。


            两种目标都一样的营养：
            蛋白质、膳食纤维、钙、铁、锌、钾、维生素和 Omega-3 [欧米伽-3] 是吃够就好，多了也还是绿的。达到九成及以上是绿的，五成到不到九成是黄的，不到五成是红的。

            钠是有上限的。没超过目标是绿的，超了但不超过一成是黄的，超了一成以上是红的。
            """,
    };

    public static NutritionDetailData FormatNutrition(DayNutrition day, string goal, double? basal)
    {
        var date = DateOnly.FromDayNumber(checked((int)day.EpochDay));
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutting = NutritionGoal.IsCut(goal);
        return new NutritionDetailData
        {
            EpochDay = day.EpochDay,
            DateLabel = date == today ? $"{date.Month}月{date.Day}日 · 今天" : $"{date.Month}月{date.Day}日",
            Cutting = cutting,
            Stats = NutritionFact.All.Select(fact => Stat(fact, day, cutting, basal)).ToList(),
            Supplements = SupplementFact.All.Select(fact => new SupplementStatVm
            {
                Name = fact.Name,
                Current = Amount(fact.Value(day.Supplements), fact.Unit),
            }).ToList(),
            ShowEmpty = day.Entries.Count == 0,
            Foods = day.Entries.Select(entry => ToFood(entry, day.Targets)).ToList(),
        };
    }

    public static IntakePanelData FormatIntake(long epochDay) => new()
    {
        EpochDay = epochDay,
        Vitamins = Choices(SupplementCatalog.Vitamins),
        Proteins = Choices(SupplementCatalog.Proteins),
        Fitness = Choices(SupplementCatalog.Fitness),
    };

    public static TargetPanelData FormatTargets(NutritionTargets targets) => new()
    {
        Calories = NutritionMath.FormatNumber(targets.Calories),
        Protein = NutritionMath.FormatNumber(targets.Protein),
        Fat = NutritionMath.FormatNumber(targets.Fat),
        Carbs = NutritionMath.FormatNumber(targets.Carbs),
        Fiber = NutritionMath.FormatNumber(targets.Fiber),
        Sodium = NutritionMath.FormatNumber(targets.Sodium),
        Calcium = NutritionMath.FormatNumber(targets.Calcium),
        Iron = NutritionMath.FormatNumber(targets.Iron),
        Zinc = NutritionMath.FormatNumber(targets.Zinc),
        Potassium = NutritionMath.FormatNumber(targets.Potassium),
        VitaminB1 = NutritionMath.FormatNumber(targets.VitaminB1),
        VitaminC = NutritionMath.FormatNumber(targets.VitaminC),
        VitaminD = NutritionMath.FormatNumber(targets.VitaminD),
        Omega3 = NutritionMath.FormatNumber(targets.Omega3),
    };

    private static NutritionCardVm FormatCard(DayNutrition day) => new()
    {
        CaloriesPair = NutritionMath.FormatPair(day.Current.Calories, day.Targets.Calories),
        ProteinPair = NutritionMath.FormatPair(day.Current.Protein, day.Targets.Protein),
        FatPair = NutritionMath.FormatPair(day.Current.Fat, day.Targets.Fat),
        CarbsPair = NutritionMath.FormatPair(day.Current.Carbs, day.Targets.Carbs),
        FiberPair = NutritionMath.FormatPair(day.Current.Fiber, day.Targets.Fiber),
        SodiumPair = NutritionMath.FormatPair(day.Current.Sodium, day.Targets.Sodium),
    };

    private static NutrientStatVm Stat(NutritionFact fact, DayNutrition day, bool cutting, double? basal)
    {
        var current = fact.Value(day.Current);
        var target = fact.Target(day.Targets);
        var tone = NutrientToneRules.Tone(fact.Role, fact.Energy, current, target, cutting, basal);
        return new NutrientStatVm
        {
            Name = fact.Name,
            Current = Amount(current, fact.Unit),
            Difference = Signed(current, target, fact.Unit),
            DifferenceColor = ToneColor(tone),
            Target = Amount(target, fact.Unit),
            Percent = NutritionMath.FormatPercent(current, target),
        };
    }

    private static Color ToneColor(NutrientTone tone) => tone switch
    {
        NutrientTone.Green => Color.FromArgb("#22C55E"),
        NutrientTone.Yellow => Color.FromArgb("#EAB308"),
        _ => Color.FromArgb("#8C3A32"),
    };

    private static IReadOnlyList<SupplementChoiceVm> Choices(IReadOnlyList<SupplementItem> items) =>
        items.Select(item => new SupplementChoiceVm
        {
            Id = item.Id,
            Name = item.Name,
            Detail = DescribeSupplement(item),
        }).ToList();

    private static string DescribeSupplement(SupplementItem item)
    {
        var parts = new List<string>();
        foreach (var fact in NutritionFact.All)
        {
            var value = fact.Value(item.PerServing);
            if (value <= 0)
                continue;
            parts.Add($"{fact.Name} {NutritionMath.FormatNumber(value)} {fact.Unit}");
        }

        foreach (var fact in SupplementFact.All)
        {
            var value = fact.Value(item.PerServingDose);
            if (value <= 0)
                continue;
            parts.Add($"{fact.Name} {NutritionMath.FormatNumber(value)} {fact.Unit}");
        }

        var dose = $"1 {item.Piece}";
        return parts.Count == 0 ? dose : $"{dose} · {string.Join("，", parts)}";
    }

    private static FoodIntakeVm ToFood(IntakeEntry entry, NutritionTargets targets)
    {
        var unit = NutritionMath.UnitLabel(entry.Unit);
        return new FoodIntakeVm
        {
            Id = entry.Id,
            Title = entry.FoodName,
            AmountLabel = $"{NutritionMath.FormatNumber(entry.Amount)} {unit}",
            Lines = Lines(entry, targets),
        };
    }

    private static IReadOnlyList<FoodNutrientVm> Lines(IntakeEntry entry, NutritionTargets targets)
    {
        if (entry.Unit != NutritionUnits.Serving)
        {
            return NutritionFact.All.Select(fact => new FoodNutrientVm
            {
                Name = fact.Name,
                Label = Share(fact.Value(entry.Nutrients), fact.Target(targets), fact.Unit),
            }).ToList();
        }

        var lines = new List<FoodNutrientVm>();
        foreach (var fact in NutritionFact.All)
        {
            var value = fact.Value(entry.Nutrients);
            if (value <= 0)
                continue;
            lines.Add(new FoodNutrientVm
            {
                Name = fact.Name,
                Label = Share(value, fact.Target(targets), fact.Unit),
            });
        }

        foreach (var fact in SupplementFact.All)
        {
            var value = fact.Value(entry.Supplements);
            if (value <= 0)
                continue;
            lines.Add(new FoodNutrientVm
            {
                Name = fact.Name,
                Label = Amount(value, fact.Unit),
            });
        }

        return lines;
    }

    private static string Amount(double value, string unit) =>
        $"{NutritionMath.FormatNumber(value)} {unit}";

    private static string Signed(double current, double target, string unit) =>
        $"{NutritionMath.FormatSigned(current, target)} {unit}";

    private static string Share(double value, double target, string unit) =>
        $"{NutritionMath.FormatNumber(value)} {unit} · {NutritionMath.FormatPercent(value, target)}";

    public static SessionPanelData FormatSession(SessionItem? item, long taskId, long epochDay, long now)
    {
        if (item is null)
            return SessionPanelData.MissingTask(taskId, epochDay);

        if (PlanCodes.IsCardio(item.Type))
            return FormatCardioSession(item, epochDay, now);

        var view = Describe(item.Status, item.CurrentSet, item.Sets, item.RestSeconds, item.RestEndsAt, now);
        return new SessionPanelData
        {
            TaskId = item.Id,
            EpochDay = epochDay,
            Title = item.Title,
            PhaseLabel = $"力量 · {view.Phase}",
            ProgressLabel = view.Progress,
            CountdownLabel = view.Countdown,
            ShowCountdown = view.Resting,
            ShowStart = item.Status == PlanCodes.NotStarted,
            ShowComplete = item.Status == PlanCodes.InProgress && !view.Resting,
            ShowDone = item.Status == PlanCodes.Done,
            Cardio = false,
            ShowStrengthTools = true,
            SetsLabel = $"{item.Sets} 组",
            RestLabel = $"间隔 {item.RestSeconds} 秒",
            Sets = item.Sets,
            RestSeconds = item.RestSeconds,
            CanDecreaseSets = item.Sets > PlanLimits.MinSets,
            CanIncreaseSets = item.Sets < PlanLimits.MaxSets,
            CanDecreaseRest = item.RestSeconds > PlanLimits.MinRestSeconds,
            CanIncreaseRest = item.RestSeconds < PlanLimits.MaxRestSeconds,
            NotStartedBackground = item.Status == PlanCodes.NotStarted ? Accent : Card,
            NotStartedText = item.Status == PlanCodes.NotStarted ? Colors.White : Ink,
            InProgressBackground = item.Status == PlanCodes.InProgress ? Accent : Card,
            InProgressText = item.Status == PlanCodes.InProgress ? Colors.White : Ink,
            DoneBackground = item.Status == PlanCodes.Done ? Accent : Card,
            DoneText = item.Status == PlanCodes.Done ? Colors.White : Ink,
        };
    }

    public static EditorPanelData FormatEditor(EditorItem item)
    {
        var type = PlanCodes.Canonical(item.Type);
        return new EditorPanelData
        {
            TaskId = item.TaskId,
            DayOfWeek = item.DayOfWeek,
            EpochDay = item.EpochDay,
            Creating = item.Creating,
            Heading = item.Creating ? "新建任务" : "编辑任务",
            Title = item.Title,
            Type = type,
            LockType = !item.Creating,
            Sets = item.Sets,
            RestSeconds = item.RestSeconds,
            DurationMinutes = item.DurationMinutes,
            Status = item.Status,
            ShowStatus = !item.Creating && PlanCodes.IsTraining(type),
            ShowDelete = !item.Creating,
            TypeLabel = TypeName(type),
        };
    }

    private static SessionPanelData FormatCardioSession(SessionItem item, long epochDay, long now)
    {
        var view = DescribeCardio(item.Status, item.DurationMinutes, item.RestEndsAt, now);
        var done = item.Status == PlanCodes.Done;
        return new SessionPanelData
        {
            TaskId = item.Id,
            EpochDay = epochDay,
            Title = item.Title,
            PhaseLabel = $"有氧 · {view.Phase}",
            ProgressLabel = view.Progress,
            CountdownLabel = view.Countdown,
            ShowCountdown = !done,
            ShowStart = item.Status == PlanCodes.NotStarted,
            ShowComplete = false,
            ShowDone = done,
            Cardio = true,
            ShowStrengthTools = false,
            DoneMessage = "今天这次有氧已经完成",
            SetsLabel = $"{item.Sets} 组",
            RestLabel = $"间隔 {item.RestSeconds} 秒",
            Sets = item.Sets,
            RestSeconds = item.RestSeconds,
            NotStartedBackground = item.Status == PlanCodes.NotStarted ? Accent : Card,
            NotStartedText = item.Status == PlanCodes.NotStarted ? Colors.White : Ink,
            InProgressBackground = item.Status == PlanCodes.InProgress ? Accent : Card,
            InProgressText = item.Status == PlanCodes.InProgress ? Colors.White : Ink,
            DoneBackground = item.Status == PlanCodes.Done ? Accent : Card,
            DoneText = item.Status == PlanCodes.Done ? Colors.White : Ink,
        };
    }

    private static TaskRowVm ToRow(TaskItem item, int dayOfWeek, long epochDay, long now)
    {
        var training = PlanCodes.IsTraining(item.Type);
        var cardio = PlanCodes.IsCardio(item.Type);
        var done = item.Status == PlanCodes.Done;
        var inProgress = item.Status == PlanCodes.InProgress;
        var strengthView = Describe(item.Status, item.CurrentSet, item.Sets, item.RestSeconds, item.RestEndsAt, now);
        var cardioView = DescribeCardio(item.Status, item.DurationMinutes, item.RestEndsAt, now);
        var unfinished = !done && !inProgress;
        var chip = unfinished ? Orange : Accent;
        var meta = cardio
            ? cardioView.Timing ? $"有氧 · {cardioView.Meta}" : done ? $"有氧 · ✓ {item.DurationMinutes} 分钟" : $"有氧 · {item.DurationMinutes} 分钟"
            : strengthView.Resting ? $"力量 · {strengthView.Meta}" : $"力量 · {(done ? "✓ " : "")}{item.Sets} 组 · 间隔 {item.RestSeconds} 秒";
        return new TaskRowVm
        {
            Id = item.Id,
            DayOfWeek = dayOfWeek,
            EpochDay = epochDay,
            Fitness = training,
            Title = item.Title,
            TypeLabel = ShortType(item.Type),
            StatusLabel = StatusLabel(item.Status),
            MetaLabel = meta,
            ShowMeta = true,
            MetaColor = done ? Color.FromArgb("#22A35A") : Color.FromArgb("#8E95A3"),
            ChipBackground = unfinished ? Color.FromArgb("#FFF8F1") : Color.FromArgb("#F3FBF6"),
            ChipBorder = chip,
            ChipForeground = unfinished ? Color.FromArgb("#E8892A") : Color.FromArgb("#1FA85A"),
            ActionLabel = done ? "完成 ✓" : inProgress ? "继续" : "开始",
            ActionBackground = done ? Accent : Dark,
            ActionForeground = Colors.White,
            ActionBorder = Colors.Transparent,
        };
    }

    private static string StatusLabel(string status) => status switch
    {
        PlanCodes.Done => "完成",
        PlanCodes.InProgress => "进行中",
        _ => "未完成",
    };

    private static string TypeName(string type) =>
        type == PlanCodes.Cardio ? "有氧训练" : "力量训练";

    private static string ShortType(string type) =>
        type == PlanCodes.Cardio ? "有氧" : "力量";

    private static (string Phase, string Progress, string Countdown, string Meta, bool Timing) DescribeCardio(
        string status,
        int minutes,
        long? endsAt,
        long now)
    {
        var clock = FormatClock(minutes * 60);
        if (status == PlanCodes.InProgress && endsAt is long end && now < end)
        {
            var countdown = FormatCountdown(end, now);
            return ("进行中", $"{minutes} 分钟", countdown, $"剩余 {countdown}", true);
        }

        if (status == PlanCodes.Done)
            return ("已完成", "今天这次有氧已经完成", clock, $"✓ {minutes} 分钟", false);

        return ("未开始", $"共 {minutes} 分钟", clock, $"{minutes} 分钟", false);
    }

    private static (string Phase, string Progress, string Countdown, string Meta, bool Resting) Describe(
        string status,
        int current,
        int sets,
        int rest,
        long? restEndsAt,
        long now)
    {
        var resting = status == PlanCodes.InProgress && restEndsAt is long end && now < end;
        if (resting && restEndsAt is long restEnd)
        {
            var countdown = FormatCountdown(restEnd, now);
            var next = Math.Min(current + 1, sets);
            return ("休息中", $"下一组是第 {next} / {sets} 组", countdown, $"第 {current} 组已完成 · 休息 {countdown}", true);
        }

        if (status == PlanCodes.InProgress)
        {
            var progress = $"第 {current} / {sets} 组";
            return ("进行中", progress, "", progress, false);
        }

        if (status == PlanCodes.Done)
            return ("已完成", "今天这组训练已经完成", "", $"{sets} 组 · 间隔 {rest} 秒", false);

        return ("未开始", $"共 {sets} 组", "", $"{sets} 组 · 间隔 {rest} 秒", false);
    }

    public static string FormatCountdown(long restEndsAt, long now)
    {
        var remain = Math.Max(0, restEndsAt - now);
        var seconds = (remain + 999) / 1000;
        return FormatClock(seconds);
    }

    public static string FormatWeightAverage(double? kilograms) =>
        kilograms is double value ? $"{NutritionMath.FormatNumber(value)} 千克" : "还没有体重";

    public static string FormatMeasure(double? centimeters) =>
        centimeters is double value ? $"{NutritionMath.FormatNumber(value)} 厘米" : "还没有记录";

    public static WeightChartData FormatWeightChart(IReadOnlyList<WeightEntry> weights, DateOnly today)
    {
        var todayKey = WeekCalendar.DayKey(today);
        var points = weights.Select(entry => ChartPoint(entry.EpochDay, entry.Kilograms, today)).ToList();
        var todayPoint = points.FirstOrDefault(point => point.EpochDay == todayKey);
        return new WeightChartData
        {
            AverageLabel = FormatWeightAverage(WeightRules.AverageOfLatest(weights)),
            TodayWeight = todayPoint?.ValueLabel ?? "",
            ShowEmpty = points.Count == 0,
            MinLabel = MinLabel(points),
            MaxLabel = MaxLabel(points),
            StartLabel = points.Count == 0 ? "" : points[0].DateLabel,
            EndLabel = points.Count == 0 ? "" : points[^1].DateLabel,
            Points = points,
        };
    }

    public static BodyChartData FormatBodyChart(IReadOnlyList<BodyMeasures> measures, DateOnly today)
    {
        var latest = measures.Count == 0 ? null : measures[^1];
        return new BodyChartData
        {
            Waist = latest is null ? "" : NutritionMath.FormatNumber(latest.WaistCm),
            LeftArm = latest is null ? "" : NutritionMath.FormatNumber(latest.LeftArmCm),
            RightArm = latest is null ? "" : NutritionMath.FormatNumber(latest.RightArmCm),
            Chest = latest is null ? "" : NutritionMath.FormatNumber(latest.ChestCm),
            LeftThigh = latest is null ? "" : NutritionMath.FormatNumber(latest.LeftThighCm),
            RightThigh = latest is null ? "" : NutritionMath.FormatNumber(latest.RightThighCm),
            Series = new[]
            {
                Series("腰围", Color.FromArgb("#22C55E"), measures, row => row.WaistCm, today),
                Series("左臂围", Color.FromArgb("#3B82F6"), measures, row => row.LeftArmCm, today),
                Series("右臂围", Color.FromArgb("#6366F1"), measures, row => row.RightArmCm, today),
                Series("胸围", Color.FromArgb("#F5A04A"), measures, row => row.ChestCm, today),
                Series("左腿围", Color.FromArgb("#EC4899"), measures, row => row.LeftThighCm, today),
                Series("右腿围", Color.FromArgb("#14B8A6"), measures, row => row.RightThighCm, today),
            },
        };
    }

    public static WeightPointPanelData FormatWeightPoint(WeightEntry entry)
    {
        var date = DateOnly.FromDayNumber(checked((int)entry.EpochDay));
        return new WeightPointPanelData
        {
            EpochDay = entry.EpochDay,
            Heading = $"{DateText(date, DateOnly.FromDateTime(DateTime.Today))}的体重",
            Weight = NutritionMath.FormatNumber(entry.Kilograms),
        };
    }

    private static BodySeriesVm Series(
        string name,
        Color line,
        IReadOnlyList<BodyMeasures> measures,
        Func<BodyMeasures, double> value,
        DateOnly today)
    {
        var points = measures.Select(row => ChartPoint(row.EpochDay, value(row), today)).ToList();
        return new BodySeriesVm
        {
            Name = name,
            ShowEmpty = points.Count == 0,
            MinLabel = MinLabel(points),
            MaxLabel = MaxLabel(points),
            StartLabel = points.Count == 0 ? "" : points[0].DateLabel,
            EndLabel = points.Count == 0 ? "" : points[^1].DateLabel,
            Line = line,
            Points = points,
        };
    }

    private static ChartPointVm ChartPoint(long epochDay, double value, DateOnly today)
    {
        var date = DateOnly.FromDayNumber(checked((int)epochDay));
        return new ChartPointVm
        {
            EpochDay = epochDay,
            Value = value,
            DateLabel = DateText(date, today),
            ValueLabel = NutritionMath.FormatNumber(value),
        };
    }

    private static string MinLabel(IReadOnlyList<ChartPointVm> points) =>
        points.Count == 0 ? "" : NutritionMath.FormatNumber(points.Min(point => point.Value));

    private static string MaxLabel(IReadOnlyList<ChartPointVm> points) =>
        points.Count == 0 ? "" : NutritionMath.FormatNumber(points.Max(point => point.Value));

    private static string DateText(DateOnly date, DateOnly today) =>
        date.Year == today.Year
            ? $"{date.Month}月{date.Day}日"
            : $"{date.Year}年{date.Month}月{date.Day}日";

    public static FoodChoicePanelData FormatFoodChoice(
        long epochDay,
        string name,
        double amount,
        string unit,
        string message,
        IReadOnlyList<FoodCandidate> similar)
    {
        return new FoodChoicePanelData
        {
            EpochDay = epochDay,
            Name = name,
            Amount = amount,
            Unit = unit,
            Message = message,
            SimilarHeading = "近似食物",
            EmptyLabel = "没有近似的食物",
            ShowEmpty = similar.Count == 0,
            Foods = similar
                .Select(food => new SimilarFoodVm
                {
                    Name = food.Name,
                    Label = food.Name,
                })
                .ToList(),
        };
    }

    public static string FormatClock(long totalSeconds)
    {
        var seconds = Math.Max(0, totalSeconds);
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }
}
