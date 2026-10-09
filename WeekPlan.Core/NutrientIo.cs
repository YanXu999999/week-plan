namespace WeekPlan;

public static class NutrientIo
{
    public static NutrientValues FromFood(FoodEntity food) => new(
        food.Calories,
        food.Protein,
        food.Fat,
        food.Carbs,
        food.Fiber,
        food.Sodium,
        food.Calcium,
        food.Iron,
        food.Zinc,
        food.Potassium,
        food.VitaminB1,
        food.VitaminC,
        food.VitaminD,
        food.Omega3);

    public static void Apply(FoodEntity food, NutrientValues values)
    {
        food.Calories = values.Calories;
        food.Protein = values.Protein;
        food.Fat = values.Fat;
        food.Carbs = values.Carbs;
        ApplyMicros(food, values);
    }

    public static void ApplyMicros(FoodEntity food, NutrientValues values)
    {
        food.Fiber = values.Fiber;
        food.Sodium = values.Sodium;
        food.Calcium = values.Calcium;
        food.Iron = values.Iron;
        food.Zinc = values.Zinc;
        food.Potassium = values.Potassium;
        food.VitaminB1 = values.VitaminB1;
        food.VitaminC = values.VitaminC;
        food.VitaminD = values.VitaminD;
        food.Omega3 = values.Omega3;
    }

    public static NutrientValues FromIntake(IntakeEntity intake) => new(
        intake.Calories,
        intake.Protein,
        intake.Fat,
        intake.Carbs,
        intake.Fiber,
        intake.Sodium,
        intake.Calcium,
        intake.Iron,
        intake.Zinc,
        intake.Potassium,
        intake.VitaminB1,
        intake.VitaminC,
        intake.VitaminD,
        intake.Omega3);

    public static void Apply(IntakeEntity intake, NutrientValues values)
    {
        intake.Calories = values.Calories;
        intake.Protein = values.Protein;
        intake.Fat = values.Fat;
        intake.Carbs = values.Carbs;
        ApplyMicros(intake, values);
    }

    public static void ApplyMicros(IntakeEntity intake, NutrientValues values)
    {
        intake.Fiber = values.Fiber;
        intake.Sodium = values.Sodium;
        intake.Calcium = values.Calcium;
        intake.Iron = values.Iron;
        intake.Zinc = values.Zinc;
        intake.Potassium = values.Potassium;
        intake.VitaminB1 = values.VitaminB1;
        intake.VitaminC = values.VitaminC;
        intake.VitaminD = values.VitaminD;
        intake.Omega3 = values.Omega3;
    }

    public static SupplementDose Dose(IntakeEntity intake) => new(
        intake.Creatine,
        intake.Bcaa,
        intake.Glutamine,
        intake.Carnitine,
        intake.Electrolyte);

    public static void Apply(IntakeEntity intake, SupplementDose dose)
    {
        intake.Creatine = dose.Creatine;
        intake.Bcaa = dose.Bcaa;
        intake.Glutamine = dose.Glutamine;
        intake.Carnitine = dose.Carnitine;
        intake.Electrolyte = dose.Electrolyte;
    }

    public static NutritionTargets FromTarget(NutritionTargetEntity row) => new(
        row.Calories,
        row.Protein,
        row.Fat,
        row.Carbs,
        row.Fiber,
        row.Sodium,
        row.Calcium,
        row.Iron,
        row.Zinc,
        row.Potassium,
        row.VitaminB1,
        row.VitaminC,
        row.VitaminD,
        row.Omega3);

    public static void Apply(NutritionTargetEntity row, TargetDraft draft)
    {
        row.Calories = draft.Calories;
        row.Protein = draft.Protein;
        row.Fat = draft.Fat;
        row.Carbs = draft.Carbs;
        row.Fiber = draft.Fiber;
        row.Sodium = draft.Sodium;
        row.Calcium = draft.Calcium;
        row.Iron = draft.Iron;
        row.Zinc = draft.Zinc;
        row.Potassium = draft.Potassium;
        row.VitaminB1 = draft.VitaminB1;
        row.VitaminC = draft.VitaminC;
        row.VitaminD = draft.VitaminD;
        row.Omega3 = draft.Omega3;
    }
}
