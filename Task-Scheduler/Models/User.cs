using SQLite;

namespace Task_Scheduler.Models
{
    /// <summary>Пользователь приложения.</summary>
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Храним хэш пароля, а не сам пароль.</summary>
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

