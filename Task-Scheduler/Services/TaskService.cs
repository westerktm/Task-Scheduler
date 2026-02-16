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

                // Подгружаем подзадачи для каждой задачи
                foreach (var task in tasks)
                {
                    var subTasks = conn.Table<SubTask>()
                        .Where(s => s.TaskId == task.Id)
                        .ToListAsync()
                        .GetAwaiter()
                        .GetResult();
                    task.SubTasks = subTasks;
                }
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

            // Сохраняем подзадачи, если они есть
            if (task.SubTasks != null && task.SubTasks.Count > 0)
            {
                foreach (var sub in task.SubTasks)
                {
                    sub.TaskId = task.Id;
                }

                conn.InsertAllAsync(task.SubTasks).GetAwaiter().GetResult();
            }
        }

        public void UpdateTask(TaskItem task)
        {
            var userId = CurrentUserId;
            if (userId <= 0)
                return;

            task.UserId = userId;
            task.LastUpdated = DateTime.Now;

            var conn = _databaseService.ConnectionAsync().GetAwaiter().GetResult();

            // Обновляем саму задачу
            conn.UpdateAsync(task).GetAwaiter().GetResult();

            // Перезаписываем подзадачи: удаляем старые и добавляем текущие
            conn.Table<SubTask>()
                .Where(s => s.TaskId == task.Id)
                .DeleteAsync()
                .GetAwaiter()
                .GetResult();

            if (task.SubTasks != null && task.SubTasks.Count > 0)
            {
                foreach (var sub in task.SubTasks)
                {
                    sub.TaskId = task.Id;
                }

                conn.InsertAllAsync(task.SubTasks).GetAwaiter().GetResult();
            }
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
                // Удаляем подзадачи
                conn.Table<SubTask>()
                    .Where(s => s.TaskId == task.Id)
                    .DeleteAsync()
                    .GetAwaiter()
                    .GetResult();

                // Удаляем саму задачу
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
