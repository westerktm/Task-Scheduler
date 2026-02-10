namespace Task_Scheduler.Models
{
    /// <summary>Уровень важности задачи</summary>
    public enum TaskImportance
    {
        Low,
        Medium,
        High
    }

    /// <summary>Повторяемость задачи</summary>
    public enum RecurrenceType
    {
        None,
        Daily,
        Weekly,
        Monthly
    }

    public class TaskItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Связь с пользователем, которому принадлежит задача.</summary>
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public TimeSpan? DueTime { get; set; }
        // Для режима диапазона
        public DateTime? DueDateFrom { get; set; }
        public TimeSpan? DueTimeFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public TimeSpan? DueTimeTo { get; set; }
        public bool IsDateRange { get; set; } = false;
        public bool IsFavorite { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
        public bool DueNotificationSent { get; set; } = false;
        public bool NotificationDismissed { get; set; } = false;

        // Напоминание: 0 = без напоминания, 5/15/30 = за N минут до дедлайна
        public int ReminderMinutes { get; set; } = 0;
        public bool ReminderNotificationSent { get; set; } = false;

        // Повторяемость
        public bool IsRecurring { get; set; } = false;
        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;

        // Важность
        public TaskImportance Importance { get; set; } = TaskImportance.Medium;

        // Помодоро: длительность сессии в минутах (0 = выключено)
        public int PomodoroDurationMinutes { get; set; } = 25;

        /// <summary>
        /// Список подзадач хранится только в памяти и не маппится напрямую в таблицу SQLite.
        /// При необходимости его можно вынести в отдельную таблицу.
        /// </summary>
        [SQLite.Ignore]
        public List<SubTask> SubTasks { get; set; } = new List<SubTask>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class SubTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
    }
}
