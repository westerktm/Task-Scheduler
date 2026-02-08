using Task_Scheduler.Models;

namespace Task_Scheduler.Services
{
    public class TaskService
    {
        private static TaskService? _instance;
        private List<TaskItem> _tasks = new List<TaskItem>();

        public static TaskService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TaskService();
                }
                return _instance;
            }
        }

        public List<TaskItem> GetTasks() => _tasks;

        public void AddTask(TaskItem task)
        {
            task.CreatedAt = DateTime.Now;
            task.LastUpdated = DateTime.Now;
            _tasks.Add(task);
        }

        public void UpdateTask(TaskItem task)
        {
            var existingTask = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existingTask != null)
            {
                var index = _tasks.IndexOf(existingTask);
                task.LastUpdated = DateTime.Now;
                _tasks[index] = task;
            }
        }

        public void DeleteTask(string taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                _tasks.Remove(task);
            }
        }

        public TaskItem? GetTaskById(string taskId) => _tasks.FirstOrDefault(t => t.Id == taskId);
    }
}
