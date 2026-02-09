using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace Task_Scheduler.Services
{
    /// <summary>
    /// Хранит и загружает настройки приложения (Preferences) и помогает применить их к ресурсам.
    /// </summary>
    public static class AppSettings
    {
        private const string KeyDefaultDisplayMode = "DefaultDisplayMode";
        private const string KeyAppTheme = "AppTheme"; // "Light" | "Dark" | "System"
        private const string KeyNotificationsEnabled = "NotificationsEnabled";
        private const string KeyQuietHoursStart = "QuietHoursStart"; // "HH:mm"
        private const string KeyQuietHoursEnd = "QuietHoursEnd";     // "HH:mm"
        private const string KeyAccentColor = "AccentColor";         // имя цвета для типов задач
        private const string KeyFontFamily = "FontFamily";           // "" | "OpenSansRegular" | "OpenSansSemibold"
        private const string KeyAvatarPath = "AvatarPath";           // путь к выбранному аватару
        private const string KeyProfileChartType = "ProfileChartType"; // "Line" | "Donut" | "Heatmap" | "Bar"

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

        /// <summary>
        /// "" означает системный шрифт.
        /// </summary>
        public static string FontFamily
        {
            get => Preferences.Default.Get(KeyFontFamily, "");
            set => Preferences.Default.Set(KeyFontFamily, value ?? "");
        }

        public static string AvatarPath
        {
            get => Preferences.Default.Get(KeyAvatarPath, "");
            set => Preferences.Default.Set(KeyAvatarPath, value ?? "");
        }

        public static string ProfileChartType
        {
            get => Preferences.Default.Get(KeyProfileChartType, "Line");
            set => Preferences.Default.Set(KeyProfileChartType, value ?? "Line");
        }

        public static (Color light, Color dark) GetAccentColors()
        {
            // Пары: Light / Dark (для тёмной темы берём более светлый вариант)
            return AccentColor switch
            {
                "Blue" => (Color.FromArgb("#0D6EFD"), Color.FromArgb("#8FB8FF")),
                "Green" => (Color.FromArgb("#198754"), Color.FromArgb("#7DDC9F")),
                "Orange" => (Color.FromArgb("#FD7E14"), Color.FromArgb("#FFB066")),
                _ => (Color.FromArgb("#512BD4"), Color.FromArgb("#ac99ea")), // Primary (фиолетовый)
            };
        }

        public static void ApplyToResources(ResourceDictionary resources)
        {
            // Font
            resources["AppFontFamily"] = string.IsNullOrWhiteSpace(FontFamily) ? null : FontFamily;

            // Accent
            var (light, dark) = GetAccentColors();
            resources["AccentLight"] = light;
            resources["AccentDark"] = dark;
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
