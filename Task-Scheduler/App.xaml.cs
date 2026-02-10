using Microsoft.Extensions.DependencyInjection;
using Task_Scheduler.Services;

namespace Task_Scheduler
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Apply persisted settings early (fonts/accent/theme)
            AppSettings.ApplyToResources(Current.Resources);
            ApplySavedTheme();
        }

        private void ApplySavedTheme()
        {
            switch (AppSettings.AppTheme)
            {
                case AppSettings.ThemeDark:
                    UserAppTheme = AppTheme.Dark;
                    break;
                case AppSettings.ThemeLight:
                    UserAppTheme = AppTheme.Light;
                    break;
                default:
                    UserAppTheme = AppTheme.Unspecified; // system
                    break;
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            TaskNotificationScheduler.Start();
            // Стартуем с экрана логина, внутри которого уже решаем, показывать ли основную оболочку
            return new Window(new LoginPage());
        }
    }
}