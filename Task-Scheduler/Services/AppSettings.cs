namespace Task_Scheduler.Services
{
    /// <summary>
    /// Хранит и загружает настройки приложения (Preferences).
    /// </summary>
    public static class AppSettings
    {
        private const string KeyDefaultDisplayMode = "DefaultDisplayMode";
        private const string KeyAppTheme = "AppTheme"; // "Light" | "Dark" | "System"
        private const string KeyNotificationsEnabled = "NotificationsEnabled";
        private const string KeyQuietHoursStart = "QuietHoursStart"; // "HH:mm"
        private const string KeyQuietHoursEnd = "QuietHoursEnd";     // "HH:mm"
        private const string KeyAccentColor = "AccentColor";         // имя цвета для типов задач

        public const string DisplayModeList = "List";
        public const string DisplayModeKanban = "Kanban";
        public const string DisplayModeCalendar = "Calendar";
        public const string DisplayModeGantt = "Gantt";

        public const string ThemeLight = "Light";
        public const string ThemeDark = "Dark";
        public const string ThemeSystem = "System";

        public static string DefaultDisplayMode
        {
            get => Preferences.Default.Get(KeyDefaultDisplayMode, DisplayModeList);
            set => Preferences.Default.Set(KeyDefaultDisplayMode, value);
        }

        public static string AppTheme
        {
            get => Preferences.Default.Get(KeyAppTheme, ThemeSystem);
            set => Preferences.Default.Set(KeyAppTheme, value);
        }

        public static bool NotificationsEnabled
        {
            get => Preferences.Default.Get(KeyNotificationsEnabled, true);
            set => Preferences.Default.Set(KeyNotificationsEnabled, value);
        }

        /// <summary>Начало тихих часов, например "22:00".</summary>
        public static string QuietHoursStart
        {
            get => Preferences.Default.Get(KeyQuietHoursStart, "22:00");
            set => Preferences.Default.Set(KeyQuietHoursStart, value);
        }

        /// <summary>Конец тихих часов, например "08:00".</summary>
        public static string QuietHoursEnd
        {
            get => Preferences.Default.Get(KeyQuietHoursEnd, "08:00");
            set => Preferences.Default.Set(KeyQuietHoursEnd, value);
        }

        public static string AccentColor
        {
            get => Preferences.Default.Get(KeyAccentColor, "Primary");
            set => Preferences.Default.Set(KeyAccentColor, value);
        }

        /// <summary>Проверяет, попадает ли текущее время в тихие часы (не показывать уведомления).</summary>
        public static bool IsInQuietHours(DateTime now)
        {
            if (!TimeSpan.TryParse(QuietHoursStart.Replace(',', '.'), out var start))
                start = new TimeSpan(22, 0, 0);
            if (!TimeSpan.TryParse(QuietHoursEnd.Replace(',', '.'), out var end))
                end = new TimeSpan(8, 0, 0);

            var t = now.TimeOfDay;
            if (start <= end)
                return t >= start && t < end;
            return t >= start || t < end;
        }
    }
}
