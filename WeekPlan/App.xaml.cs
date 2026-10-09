using SQLitePCL;
using WeekPlan.App.Ui;

namespace WeekPlan.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        try
        {
            Batteries_V2.Init();

            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WeekPlan");
            Directory.CreateDirectory(folder);
            var database = new WeekPlan.AppDatabase(Path.Combine(folder, "week-plan.db"));
            // 建表续延不能回到界面线程，否则启动会停在启动图上。
            Task.Run(database.InitializeAsync).GetAwaiter().GetResult();
            var plans = new WeekPlan.WeekPlanService(database);
            var nutrition = new WeekPlan.NutritionService(database);
            var profile = new WeekPlan.ProfileService(database);
            var body = new WeekPlan.BodyMetricsService(database);
            MainPage = new RootPage(new AppCoordinator(plans, nutrition, profile, body));
        }
        catch (Exception ex)
        {
            MainPage = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#F7F8FB"),
                Content = new ScrollView
                {
                    Content = new Label
                    {
                        Text = ex.ToString(),
                        TextColor = Color.FromArgb("#1B1F2A"),
                        Margin = 24,
                    },
                },
            };
        }
    }
}
