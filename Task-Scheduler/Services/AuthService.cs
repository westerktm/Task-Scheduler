using System.Security.Cryptography;
using System.Text;
using SQLite;
using Task_Scheduler.Models;

namespace Task_Scheduler.Services
{
    /// <summary>
    /// Простая авторизация: регистрация пользователя и вход по логину/паролю.
    /// </summary>
    public class AuthService
    {
        private static AuthService? _instance;
        private static readonly object _lock = new();

        public static AuthService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new AuthService();
                    }
                }
                return _instance;
            }
        }

        private readonly DatabaseService _databaseService;

        public User? CurrentUser { get; private set; }

        private AuthService()
        {
            _databaseService = DatabaseService.Instance;
        }

        public async Task InitializeFromSettingsAsync()
        {
            var savedUserId = AppSettings.CurrentUserId;
            if (savedUserId <= 0)
                return;

            var conn = await _databaseService.ConnectionAsync();
            CurrentUser = await conn.Table<User>().FirstOrDefaultAsync(u => u.Id == savedUserId);

            if (CurrentUser == null)
            {
                AppSettings.CurrentUserId = 0;
                AppSettings.CurrentUserName = string.Empty;
            }
        }

        public bool IsAuthenticated => CurrentUser != null;

        public async Task<(bool ok, string error)> RegisterAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return (false, "Введите логин и пароль");

            var conn = await _databaseService.ConnectionAsync();

            var existing = await conn.Table<User>().FirstOrDefaultAsync(u => u.UserName == userName);
            if (existing != null)
                return (false, "Пользователь с таким именем уже существует");

            var user = new User
            {
                UserName = userName,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            await conn.InsertAsync(user);
            CurrentUser = user;
            AppSettings.CurrentUserId = user.Id;
            AppSettings.CurrentUserName = user.UserName;

            return (true, string.Empty);
        }

        public async Task<(bool ok, string error)> LoginAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return (false, "Введите логин и пароль");

            var conn = await _databaseService.ConnectionAsync();

            var user = await conn.Table<User>().FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
                return (false, "Пользователь не найден");

            var hash = HashPassword(password);
            if (!string.Equals(hash, user.PasswordHash, StringComparison.Ordinal))
                return (false, "Неверный пароль");

            CurrentUser = user;
            AppSettings.CurrentUserId = user.Id;
            AppSettings.CurrentUserName = user.UserName;

            return (true, string.Empty);
        }

        public void Logout()
        {
            CurrentUser = null;
            AppSettings.CurrentUserId = 0;
            AppSettings.CurrentUserName = string.Empty;
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}

