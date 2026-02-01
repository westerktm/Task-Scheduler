namespace Task_Scheduler.Services
{
#if WINDOWS
    public class NotificationService : INotificationService
    {
        public void ShowNotification(string title, string message)
        {
            try
            {
                new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .Show();
            }
            catch
            {
                // Игнорируем ошибки показа уведомлений (например, если приложение не упаковано)
            }
        }
    }
#else
    public class NotificationService : INotificationService
    {
        public void ShowNotification(string title, string message)
        {
            // Заглушка для платформ, отличных от Windows
        }
    }
#endif
}
