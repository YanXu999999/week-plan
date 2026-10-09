using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace WeekPlan.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ScreenOrientation = ScreenOrientation.Portrait, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density, WindowSoftInputMode = SoftInput.AdjustResize)]
public class MainActivity : MauiAppCompatActivity
{
    public static event Action<MotionEvent>? OutsideTap;

    public override bool DispatchTouchEvent(MotionEvent? e)
    {
        if (e is not null && e.ActionMasked == MotionEventActions.Down)
            OutsideTap?.Invoke(e);
        return base.DispatchTouchEvent(e);
    }
}
