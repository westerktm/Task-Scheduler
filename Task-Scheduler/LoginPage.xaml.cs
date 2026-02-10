using Task_Scheduler.Services;

namespace Task_Scheduler
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService = AuthService.Instance;

        public LoginPage()
        {
            InitializeComponent();
            // Попытка автологина
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await _authService.InitializeFromSettingsAsync();
            if (_authService.IsAuthenticated)
            {
                await NavigateToMainAsync();
            }
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;
            var userName = UserNameEntry.Text?.Trim() ?? string.Empty;
            var password = PasswordEntry.Text ?? string.Empty;

            var (ok, error) = await _authService.LoginAsync(userName, password);
            if (!ok)
            {
                ErrorLabel.Text = error;
                ErrorLabel.IsVisible = true;
                return;
            }

            await NavigateToMainAsync();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;
            var userName = UserNameEntry.Text?.Trim() ?? string.Empty;
            var password = PasswordEntry.Text ?? string.Empty;

            var (ok, error) = await _authService.RegisterAsync(userName, password);
            if (!ok)
            {
                ErrorLabel.Text = error;
                ErrorLabel.IsVisible = true;
                return;
            }

            await NavigateToMainAsync();
        }

        private static async Task NavigateToMainAsync()
        {
            // После логина переключаем MainPage на Shell с основной страницей
            Application.Current!.MainPage = new AppShell();
            await Task.CompletedTask;
        }
    }
}

