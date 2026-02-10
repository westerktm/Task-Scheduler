using Task_Scheduler.Models;

namespace Task_Scheduler.Services
{
    /// <summary>
    /// Сервис работы с задачами. Хранит задачи в SQLite и фильтрует их по текущему пользователю.
    /// API оставлен синхронным, как было в исходном коде.
    /// </summary>
    public class TaskService
    {
        private static TaskService? _instance;
        private static readonly object _lock = new();

        private readonly DatabaseService _databaseService;

        private TaskService()
        {
            _databaseService = DatabaseService.Instance;
        }

        public static TaskService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new TaskService();
                    }
                }
                return _instance;
            }
        }

        private int CurrentUserId => AppSettings.CurrentUserId;

        public List<TaskItem> GetTasks()
        {
            try
            {
                var userId = CurrentUserId;
                if (userId <= 0)
                    return new List<TaskItem>();

                var conn = _databaseService.ConnectionAsync().GetAwaiter().GetResult();
                var tasks = conn.Table<TaskItem>()
                    .Where(t => t.UserId == userId)
                    .ToListAsync()
                    .GetAwaiter()
                    .GetResult();
                return tasks;
            }
            catch
            {
                return new List<TaskItem>();
            }
        }

        public void AddTask(TaskItem task)
        {
            var userId = CurrentUserId;
            if (userId <= 0)
                return;

            task.UserId = userId;
            task.CreatedAt = DateTime.Now;
            task.LastUpdated = DateTime.Now;

            var conn = _databaseService.ConnectionAsync().GetAwaiter().GetResult();
            conn.InsertAsync(task).GetAwaiter().GetResult();
        }

        public void UpdateTask(TaskItem task)
        {
            var userId = CurrentUserId;
            if (userId <= 0)
                return;

            task.UserId = userId;
            task.LastUpdated = DateTime.Now;

            var conn = _databaseService.ConnectionAsync().GetAwaiter().GetResult();
            conn.UpdateAsync(task).GetAwaiter().GetResult();
        }

        public void DeleteTask(string taskId)
        {
            var userId = CurrentUserId;
            if (userId <= 0)
                return;

            var conn = _databaseService.ConnectionAsync().GetAwaiter().GetResult();
            var task = conn.Table<TaskItem>()
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId)
                .GetAwaiter()
                .GetResult();

            if (task != null)
            {
                conn.DeleteAsync(task).GetAwaiter().GetResult();
            }
        }

        public TaskItem? GetTaskById(string taskId)
        {
            var userId = CurrentUserId;
            if (userId <= 0)
                return null;

            var conn = _databaseService.ConnectionAsync().GetAwaiter().GetResult();
            var task = conn.Table<TaskItem>()
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId)
                .GetAwaiter()
                .GetResult();
            return task;
        }
    }
}
