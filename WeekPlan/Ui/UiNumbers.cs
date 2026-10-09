using System.Globalization;

namespace WeekPlan.App.Ui;

public static class UiNumbers
{
    public static bool TryParse(string? text, out double value)
    {
        value = 0;
        var trimmed = text?.Trim() ?? "";
        if (trimmed.Length == 0)
            return false;
        return double.TryParse(trimmed, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            || double.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryParseInt(string? text, out int value)
    {
        value = 0;
        var trimmed = text?.Trim() ?? "";
        if (trimmed.Length == 0)
            return false;
        return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out value)
            || int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
