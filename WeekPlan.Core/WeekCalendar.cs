namespace WeekPlan;

public static class WeekCalendar
{
    public static int PlanDay(DateOnly date) =>
        date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;

    public static DateOnly DateForPlanDay(int planDay, DateOnly today)
    {
        var monday = today.AddDays(-(PlanDay(today) - 1));
        return monday.AddDays(planDay - 1);
    }

    public static long DayKey(DateOnly date) => date.DayNumber;
}

public static class AppClock
{
    public static long NowMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
