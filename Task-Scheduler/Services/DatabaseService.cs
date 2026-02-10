using SQLite;
using Task_Scheduler.Models;

namespace Task_Scheduler.Services
{
    /// <summary>
    /// Отвечает за создание и доступ к единственной базе SQLite.
    /// </summary>
    public class DatabaseService
    {
        private static DatabaseService? _instance;
        private static readonly object _lock = new();

        public static DatabaseService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new DatabaseService();
                    }
                }
                return _instance;
            }
        }

        private readonly string _dbPath;
        private SQLiteAsyncConnection? _connection;

        private DatabaseService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "taskscheduler.db3");
        }

        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_connection != null)
                return _connection;

            _connection = new SQLiteAsyncConnection(_dbPath);

            // Создаём таблицы при первом обращении
            await _connection.CreateTableAsync<User>();
            await _connection.CreateTableAsync<TaskItem>();

            return _connection;
        }

        public async Task<SQLiteAsyncConnection> ConnectionAsync() => await GetConnectionAsync();
    }
}

