namespace Task_Scheduler.Models
{
    public class TaskItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public TimeSpan? DueTime { get; set; }
        // Для режима диапазона
        public DateTime? DueDateFrom { get; set; }
        public TimeSpan? DueTimeFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public TimeSpan? DueTimeTo { get; set; }
        public bool IsDateRange { get; set; } = false; // true если выбран режим "от и до"
        public bool IsFavorite { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
        public bool DueNotificationSent { get; set; } = false;
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
