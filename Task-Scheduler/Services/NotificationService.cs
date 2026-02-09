using System;

namespace Task_Scheduler.Services
{
#if WINDOWS
    public class NotificationService : INotificationService
    {
        public void ShowNotification(string title, string message)
        {
            try
            {
                // Используем Windows Community Toolkit для показа уведомлений
                var toast = new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .SetToastScenario(Microsoft.Toolkit.Uwp.Notifications.ToastScenario.Default);
                
                toast.Show();
                
                // Также выводим в консоль для отладки
                System.Diagnostics.Debug.WriteLine($"Notification: {title} - {message}");
            }
            catch (Exception ex)
            {
                // Выводим ошибку для отладки
                System.Diagnostics.Debug.WriteLine($"Notification error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                // Пытаемся показать через альтернативный способ
                try
                {
                    Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert(title, message, "OK");
                }
                catch
                {
                    // Игнорируем ошибки
                }
            }
        }
    }
#else
    public class NotificationService : INotificationService
    {
        public void ShowNotification(string title, string message)
        {
            // Для других платформ используем DisplayAlert
            try
            {
                Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert(title, message, "OK");
            }
            catch
            {
                // Игнорируем ошибки
            }
        }
    }
#endif
}
