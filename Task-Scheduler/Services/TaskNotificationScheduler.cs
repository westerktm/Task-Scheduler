using System.Diagnostics;
using Task_Scheduler.Models;

namespace Task_Scheduler.Services
{
    /// <summary>
    /// Проверяет задачи на наступление времени выполнения и показывает уведомления.
    /// На Windows запланированные уведомления не поддерживаются, поэтому используется таймер.
    /// </summary>
    public class TaskNotificationScheduler
    {
        private static TaskNotificationScheduler? _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// Вызывается при отправке уведомления о задаче.
        /// </summary>
        public static event EventHandler? NotificationSent;

        private readonly INotificationService _notificationService;
        private readonly TaskService _taskService;
        private System.Threading.Timer? _timer;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

        public TaskNotificationScheduler()
        {
            _notificationService = new NotificationService();
            _taskService = TaskService.Instance;
        }

        public static void Start()
        {
            lock (_lock)
            {
                _instance ??= new TaskNotificationScheduler();
                _instance.StartInternal();
            }
        }

        private void StartInternal()
        {
            _timer?.Dispose();
            _timer = new System.Threading.Timer(
                _ => CheckAndNotify(),
                null,
                TimeSpan.FromSeconds(10), // Первая проверка через 10 секунд после запуска
                _checkInterval);
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private void CheckAndNotify()
        {
            try
            {
                var now = DateTime.Now;
                var tasks = _taskService.GetTasks();

                foreach (var task in tasks)
                {
                    // Пропускаем выполненные задачи
                    if (task.IsCompleted) continue;
                    
                    // Пропускаем задачи, для которых уже отправлено уведомление или пользователь удалил уведомление
                    if (task.DueNotificationSent || task.NotificationDismissed) continue;

                    DateTime? dueDateTime = GetDueDateTime(task);
                    if (dueDateTime.HasValue && now >= dueDateTime.Value)
                    {
                        _notificationService.ShowNotification(
                            "Task Scheduler",
                            $"Время выполнения задачи: {task.Title}");

                        task.DueNotificationSent = true;
                        _taskService.UpdateTask(task);

                        // Уведомляем об изменении, чтобы иконка обновилась сразу (в UI-потоке)
                        Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() =>
                            NotificationSent?.Invoke(this, EventArgs.Empty));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TaskNotificationScheduler error: {ex.Message}");
            }
        }

        private static DateTime? GetDueDateTime(TaskItem task)
        {
            if (task.IsDateRange)
            {
                if (task.DueDateFrom.HasValue && task.DueTimeFrom.HasValue)
                {
                    return task.DueDateFrom.Value.Date + task.DueTimeFrom.Value;
                }
                if (task.DueDateFrom.HasValue)
                {
                    return task.DueDateFrom.Value.Date;
                }
            }
            else
            {
                if (task.DueDate.HasValue && task.DueTime.HasValue)
                {
                    return task.DueDate.Value.Date + task.DueTime.Value;
                }
                if (task.DueDate.HasValue)
                {
                    return task.DueDate.Value.Date;
                }
            }
            return null;
        }
    }
}
