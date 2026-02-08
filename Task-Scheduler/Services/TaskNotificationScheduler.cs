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
                if (!AppSettings.NotificationsEnabled)
                    return;
                if (AppSettings.IsInQuietHours(DateTime.Now))
                    return;

                var now = DateTime.Now;
                var tasks = _taskService.GetTasks().ToList();

                foreach (var task in tasks)
                {
                    // Пропускаем выполненные задачи
                    if (task.IsCompleted) continue;
                    
                    // Пропускаем задачи, которые пользователь удалил из уведомлений
                    if (task.NotificationDismissed) continue;

                    DateTime? dueDateTime = GetDueDateTime(task);
                    if (!dueDateTime.HasValue) continue;

                    var timeUntilDue = dueDateTime.Value - now;

                    // Напоминание за N минут до дедлайна
                    if (task.ReminderMinutes > 0 && !task.ReminderNotificationSent)
                    {
                        var reminderThreshold = TimeSpan.FromMinutes(task.ReminderMinutes);
                        if (timeUntilDue <= reminderThreshold && timeUntilDue > TimeSpan.Zero)
                        {
                            _notificationService.ShowNotification(
                                "Task Scheduler",
                                $"Напоминание: {task.Title} через {task.ReminderMinutes} мин.");
                            task.ReminderNotificationSent = true;
                            _taskService.UpdateTask(task);
                            Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() =>
                                NotificationSent?.Invoke(this, EventArgs.Empty));
                        }
                    }

                    // Основное уведомление о наступлении дедлайна
                    if (task.DueNotificationSent) continue;
                    if (now >= dueDateTime.Value)
                    {
                        _notificationService.ShowNotification(
                            "Task Scheduler",
                            $"Время выполнения задачи: {task.Title}");

                        task.DueNotificationSent = true;
                        _taskService.UpdateTask(task);

                        // Для повторяющихся задач: создать следующее вхождение
                        if (task.IsRecurring && task.Recurrence != RecurrenceType.None)
                        {
                            CreateNextRecurrence(task);
                        }

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

        private void CreateNextRecurrence(TaskItem task)
        {
            var dueDateTime = GetDueDateTime(task);
            if (!dueDateTime.HasValue) return;

            var nextDue = task.Recurrence switch
            {
                RecurrenceType.Daily => dueDateTime.Value.AddDays(1),
                RecurrenceType.Weekly => dueDateTime.Value.AddDays(7),
                RecurrenceType.Monthly => dueDateTime.Value.AddMonths(1),
                _ => dueDateTime.Value
            };

            var nextTask = new TaskItem
            {
                Title = task.Title,
                Description = task.Description,
                IsDateRange = task.IsDateRange,
                ReminderMinutes = task.ReminderMinutes,
                IsRecurring = task.IsRecurring,
                Recurrence = task.Recurrence,
                Importance = task.Importance,
                PomodoroDurationMinutes = task.PomodoroDurationMinutes,
                SubTasks = task.SubTasks.Select(s => new SubTask { Title = s.Title, IsCompleted = false }).ToList()
            };

            if (task.IsDateRange && task.DueDateFrom.HasValue && task.DueDateTo.HasValue)
            {
                var span = task.DueDateTo.Value - task.DueDateFrom.Value;
                nextTask.DueDateFrom = nextDue.Date;
                nextTask.DueTimeFrom = task.DueTimeFrom;
                nextTask.DueDateTo = nextDue.Date + span;
                nextTask.DueTimeTo = task.DueTimeTo;
            }
            else if (task.IsDateRange && task.DueDateFrom.HasValue)
            {
                nextTask.DueDateFrom = nextDue.Date;
                nextTask.DueTimeFrom = task.DueTimeFrom;
                nextTask.DueDateTo = null;
                nextTask.DueTimeTo = task.DueTimeTo;
            }
            else
            {
                nextTask.DueDate = nextDue.Date;
                nextTask.DueTime = task.DueTime;
            }

            _taskService.AddTask(nextTask);
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
