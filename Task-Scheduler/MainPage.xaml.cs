using Task_Scheduler.Models;
using Task_Scheduler.Services;

namespace Task_Scheduler
{
    public partial class MainPage : ContentPage
    {
        private bool _isMenuOpen = false;
        private bool _isSettingsOpen = false;
        private bool _isNotificationsOpen = false;
        private bool _showFavoritesOnly = false;
        private bool _isTodayMode = false;
        private string _currentSortOption = "Custom order";
        private string _displayMode = "List"; // List, Kanban, Calendar, Gantt
        private System.Threading.CancellationTokenSource? _pomodoroCts;
        private string? _activePomodoroTaskId;

        public MainPage()
        {
            InitializeComponent();

            ApplySavedTheme();
            _displayMode = AppSettings.DefaultDisplayMode;

            UpdateNotificationIcon();
            LoadAvatar();
            
            // Подписываемся на изменение размера окна для адаптивности
            this.SizeChanged += OnPageSizeChanged;
            UpdateLayoutForSize();
            
            // Убеждаемся, что панель уведомлений правильно инициализирована
            if (NotificationsOverlay != null)
            {
                NotificationsOverlay.IsVisible = false;
                NotificationsOverlay.InputTransparent = true;
            }
            if (NotificationsPanel != null)
            {
                NotificationsPanel.IsVisible = false;
                NotificationsPanel.Opacity = 0;
            }
            
            // Убеждаемся, что уведомления включены по умолчанию
            if (!AppSettings.NotificationsEnabled)
            {
                // Если по какой-то причине уведомления выключены, включаем их
                AppSettings.NotificationsEnabled = true;
                System.Diagnostics.Debug.WriteLine("Notifications were disabled, enabling by default");
            }
        }

        private static bool IsDarkTheme()
        {
            var app = Application.Current;
            return app?.RequestedTheme == AppTheme.Dark || app?.UserAppTheme == AppTheme.Dark;
        }

        private static Color TextPrimary(bool isDark) => isDark ? Colors.White : Colors.Black;
        private static Color TextSecondary(bool isDark) => isDark ? Color.FromArgb("#CFCFCF") : Colors.Gray;
        private static Color TextAccent(bool isDark) => isDark ? Color.FromArgb("#9DB7FF") : Colors.DarkBlue;

        private static Color TaskCardBgForImportance(TaskImportance importance, bool isDark)
        {
            if (!isDark)
                return GetImportanceBgColor(importance);

            // Тёмная палитра (слегка тонированная под важность)
            return importance switch
            {
                TaskImportance.High => Color.FromArgb("#3A1F1F"),
                TaskImportance.Low => Color.FromArgb("#1F3A27"),
                _ => Color.FromArgb("#3A331F")
            };
        }

        private static Color TaskCardBgCompleted(bool isDark) => isDark ? Color.FromArgb("#262626") : Colors.White;

        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            UpdateLayoutForSize();
            // Обновляем задачи при изменении размера, чтобы применить новые размеры
            RefreshTasks();
        }

        private void UpdateLayoutForSize()
        {
            // Получаем ширину страницы
            var width = this.Width;
            var height = this.Height;
            
            // Адаптируем размеры панелей для маленьких экранов
            if (width > 0)
            {
                // Адаптируем панель настроек
                if (SettingsPanel != null && width < 600)
                {
                    var panelWidth = Math.Max(250, width - 40);
                    SettingsPanel.WidthRequest = panelWidth;
                    SettingsPanel.MaximumWidthRequest = panelWidth;
                    // Обновляем позицию в AbsoluteLayout
                    AbsoluteLayout.SetLayoutBounds(SettingsPanel, new Rect(0.95, 60, panelWidth, 420));
                }
                
                // Адаптируем панель уведомлений
                if (NotificationsPanel != null && width < 600)
                {
                    var panelWidth = Math.Max(300, width - 20);
                    NotificationsPanel.WidthRequest = panelWidth;
                    NotificationsPanel.MaximumWidthRequest = panelWidth;
                    NotificationsPanel.Margin = new Thickness(10, 60, 10, 0);
                }
                
                // Адаптируем боковое меню
                if (SideMenu != null && width < 600)
                {
                    var menuWidth = Math.Min(250, width * 0.8);
                    SideMenu.WidthRequest = menuWidth;
                    SideMenu.MaximumWidthRequest = menuWidth;
                }
            }
        }

        private async void OnAvatarClicked(object sender, EventArgs e)
        {
            // Открываем страницу профиля, при этом MainPage и все её кнопки/иконки остаются в стеке навигации
            await Shell.Current.GoToAsync(nameof(ProfilePage));
        }

        private async void OnEditAvatarClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите изображение для аватара",
                    FileTypes = FilePickerFileType.Images
                });

                if (result == null)
                    return;

                var ext = Path.GetExtension(result.FileName);
                if (string.IsNullOrWhiteSpace(ext))
                    ext = ".png";

                var targetPath = Path.Combine(FileSystem.AppDataDirectory, "avatar" + ext);

                await using (var sourceStream = await result.OpenReadAsync())
                await using (var targetStream = File.Open(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await sourceStream.CopyToAsync(targetStream);
                }

                AppSettings.AvatarPath = targetPath;
                LoadAvatar();
            }
            catch
            {
                // Игнорируем ошибки выбора/копирования файла
            }
        }

        private void LoadAvatar()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(AppSettings.AvatarPath) &&
                    File.Exists(AppSettings.AvatarPath))
                {
                    AvatarImageButton.Source = ImageSource.FromFile(AppSettings.AvatarPath);
                }
            }
            catch
            {
                // Игнорируем ошибки чтения
            }
        }

        private async void OnNotificationsClicked(object sender, EventArgs e)
        {
            try
            {
                _isNotificationsOpen = !_isNotificationsOpen;
                
                // Закрываем панель настроек при открытии уведомлений
                if (_isNotificationsOpen)
                {
                    _isSettingsOpen = false;
                    if (SettingsPanel != null)
                    {
                        SettingsPanel.IsVisible = false;
                        SettingsPanel.Opacity = 0;
                    }
                    
                    // Обновляем уведомления перед показом
                    RefreshNotifications();
                    
                    // Показываем overlay и панель
                    if (NotificationsOverlay != null)
                    {
                        NotificationsOverlay.IsVisible = true;
                        NotificationsOverlay.InputTransparent = false;
                    }
                    
                    if (NotificationsPanel != null)
                    {
                        // Устанавливаем правильную позицию перед показом
                        var pageWidth = this.Width;
                        if (pageWidth > 0)
                        {
                            var panelWidth = Math.Min(350, pageWidth - 20);
                            NotificationsPanel.WidthRequest = panelWidth;
                            NotificationsPanel.Margin = new Thickness(10, 60, 10, 0);
                        }
                        
                        // Убеждаемся, что панель видима и правильно позиционирована
                        NotificationsPanel.IsVisible = true;
                        NotificationsPanel.Opacity = 0;
                        NotificationsPanel.Scale = 0.8;
                        NotificationsPanel.TranslationX = 30;
                        
                        // Анимация затемнения фона
                        if (NotificationsOverlay != null)
                        {
                            NotificationsOverlay.Opacity = 0;
                        }
                        
                        // Небольшая задержка перед анимацией, чтобы убедиться, что элемент отрендерился
                        await Task.Delay(50);
                        
                        // Плавная анимация появления: затемнение фона + появление панели с масштабированием и сдвигом
                        var overlayAnimation = NotificationsOverlay?.FadeTo(1, 300, Easing.CubicOut) ?? Task.CompletedTask;
                        var panelFadeAnimation = NotificationsPanel.FadeTo(1, 300, Easing.CubicOut);
                        var panelScaleAnimation = NotificationsPanel.ScaleTo(1, 300, Easing.CubicOut);
                        var panelTranslateAnimation = NotificationsPanel.TranslateTo(0, 0, 300, Easing.CubicOut);
                        
                        await Task.WhenAll(overlayAnimation, panelFadeAnimation, panelScaleAnimation, panelTranslateAnimation);
                    }
                }
                else
                {
                    // Плавная анимация закрытия
                    if (NotificationsPanel != null)
                    {
                        var panelFadeAnimation = NotificationsPanel.FadeTo(0, 250, Easing.CubicIn);
                        var panelScaleAnimation = NotificationsPanel.ScaleTo(0.8, 250, Easing.CubicIn);
                        var panelTranslateAnimation = NotificationsPanel.TranslateTo(30, 0, 250, Easing.CubicIn);
                        var overlayAnimation = NotificationsOverlay?.FadeTo(0, 250, Easing.CubicIn) ?? Task.CompletedTask;
                        
                        await Task.WhenAll(panelFadeAnimation, panelScaleAnimation, panelTranslateAnimation, overlayAnimation);
                        
                        NotificationsPanel.IsVisible = false;
                        NotificationsPanel.Opacity = 0;
                        NotificationsPanel.Scale = 1;
                        NotificationsPanel.TranslationX = 0;
                    }
                    
                    if (NotificationsOverlay != null)
                    {
                        NotificationsOverlay.IsVisible = false;
                        NotificationsOverlay.InputTransparent = true;
                        NotificationsOverlay.Opacity = 0;
                    }
                    
                    // Обновляем иконку при закрытии панели
                    UpdateNotificationIcon();
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку для отладки
                System.Diagnostics.Debug.WriteLine($"Ошибка при открытии/закрытии уведомлений: {ex.Message}");
            }
        }

        private void OnNotificationsOverlayTapped(object sender, EventArgs e)
        {
            // Закрываем панель при клике вне её (только если клик был по overlay, а не по панели)
            if (_isNotificationsOpen)
            {
                // Проверяем, что клик был именно по overlay, а не по панели
                // Если панель видима, закрываем её
                if (NotificationsPanel.IsVisible)
                {
                    OnNotificationsClicked(sender, e);
                }
            }
        }

        private void RefreshNotifications()
        {
            NotificationsContainer.Children.Clear();
            
            // Показываем задачи, для которых пришло уведомление (напоминание или дедлайн) и которые не были удалены
            var tasks = TaskService.Instance.GetTasks()
                .Where(t => (t.DueNotificationSent || t.ReminderNotificationSent) && !t.IsCompleted && !t.NotificationDismissed)
                .ToList();

            var now = DateTime.Now;
            var notificationTasks = new List<(TaskItem task, DateTime dueDate, string status)>();

            foreach (var task in tasks)
            {
                DateTime? dueDateTime = GetTaskDueDateTime(task);
                if (dueDateTime.HasValue)
                {
                    var timeUntilDue = dueDateTime.Value - now;
                    string status = timeUntilDue < TimeSpan.Zero ? "Текущая задача" : "Скоро";
                    notificationTasks.Add((task, dueDateTime.Value, status));
                }
            }

            // Сортируем: сначала просроченные, потом по дате
            notificationTasks = notificationTasks
                .OrderByDescending(t => t.status == "Текущая задача")
                .ThenBy(t => t.dueDate)
                .ToList();

            if (notificationTasks.Count == 0)
            {
                var noNotificationsLabel = new Label
                {
                    Text = "Нет активных уведомлений",
                    FontSize = 14,
                    TextColor = Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                NotificationsContainer.Children.Add(noNotificationsLabel);
                UpdateNotificationIcon();
                return;
            }

            // Добавляем кнопку "Очистить все" если есть уведомления
            var clearAllButton = new Button
            {
                Text = "Очистить все",
                BackgroundColor = Color.FromArgb("#FF5252"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Padding = new Thickness(10, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            clearAllButton.Clicked += async (s, e) =>
            {
                bool confirm = await DisplayAlert(
                    "Очистить все уведомления",
                    "Вы уверены, что хотите удалить все уведомления?",
                    "Да",
                    "Нет");
                
                if (confirm)
                {
                    foreach (var (task, _, _) in notificationTasks)
                    {
                        // Устанавливаем флаг, что пользователь явно удалил уведомление
                        task.NotificationDismissed = true;
                        task.DueNotificationSent = false;
                        task.ReminderNotificationSent = false;
                        TaskService.Instance.UpdateTask(task);
                    }
                    RefreshNotifications();
                }
            };
            NotificationsContainer.Children.Add(clearAllButton);

            foreach (var (task, dueDate, status) in notificationTasks)
            {
                var notificationFrame = CreateNotificationFrame(task, dueDate, status);
                NotificationsContainer.Children.Add(notificationFrame);
            }

            UpdateNotificationIcon();
        }

        private static DateTime? GetTaskDueDateTime(TaskItem task)
        {
            if (task.IsDateRange && task.DueDateFrom.HasValue)
            {
                return task.DueDateFrom.Value.Date + (task.DueTimeFrom ?? TimeSpan.Zero);
            }
            if (!task.IsDateRange && task.DueDate.HasValue)
            {
                return task.DueDate.Value.Date + (task.DueTime ?? TimeSpan.Zero);
            }
            return null;
        }

        private Frame CreateNotificationFrame(TaskItem task, DateTime dueDate, string status)
        {
            var isDark = IsDarkTheme();
            var primary = TextPrimary(isDark);
            var secondary = TextSecondary(isDark);

            var frame = new Frame
            {
                BackgroundColor = isDark
                    ? (status == "Текущая задача" ? Color.FromArgb("#1F2A33") : Color.FromArgb("#202020"))
                    : Color.FromArgb("#E3F2FD"),
                BorderColor = isDark
                    ? Color.FromArgb("#3A3A3A")
                    : Colors.Blue,
                CornerRadius = 8,
                Padding = 12,
                Margin = new Thickness(0, 0, 0, 8),
                HasShadow = true
            };

            var mainLayout = new StackLayout { Spacing = 6 };

            // Верхняя часть с заголовком и кнопкой удаления
            var headerLayout = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            // Заголовок задачи
            var titleLabel = new Label
            {
                Text = task.Title,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = primary
            };
            Grid.SetColumn(titleLabel, 0);
            headerLayout.Children.Add(titleLabel);

            // Кнопка удаления уведомления
            var deleteButton = new Button
            {
                Text = "✕",
                BackgroundColor = Colors.Transparent,
                TextColor = secondary,
                FontSize = 18,
                WidthRequest = 30,
                HeightRequest = 30,
                Padding = 0,
                CornerRadius = 15
            };
            deleteButton.Clicked += (s, e) =>
            {
                // Устанавливаем флаг, что пользователь явно удалил уведомление
                task.NotificationDismissed = true;
                task.DueNotificationSent = false;
                TaskService.Instance.UpdateTask(task);
                RefreshNotifications();
            };
            Grid.SetColumn(deleteButton, 1);
            headerLayout.Children.Add(deleteButton);

            mainLayout.Children.Add(headerLayout);

            // Статус и дата
            var statusLabel = new Label
            {
                Text = $"{status} • {dueDate:dd.MM.yyyy HH:mm}",
                FontSize = 12,
                TextColor = secondary
            };
            mainLayout.Children.Add(statusLabel);

            // Описание (если есть)
            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                var descriptionLabel = new Label
                {
                    Text = task.Description,
                    FontSize = 12,
                    TextColor = secondary,
                    LineBreakMode = LineBreakMode.WordWrap,
                    MaxLines = 2
                };
                mainLayout.Children.Add(descriptionLabel);
            }

            frame.Content = mainLayout;
            return frame;
        }

        private void UpdateNotificationIcon()
        {
            // Проверяем наличие непрочитанных уведомлений (напоминание или дедлайн, не удаленных пользователем)
            var hasUnreadNotifications = TaskService.Instance.GetTasks()
                .Any(t => (t.DueNotificationSent || t.ReminderNotificationSent) && !t.IsCompleted && !t.NotificationDismissed);

            // Подбираем иконки в зависимости от темы приложения
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark ||
                         Application.Current?.UserAppTheme == AppTheme.Dark;

            if (hasUnreadNotifications)
            {
                NotificationsImageButton.Source = isDark
                    ? "notificationsalertbt.png"
                    : "notificationsalert.png";
            }
            else
            {
                NotificationsImageButton.Source = isDark
                    ? "notiblacktheme.png"
                    : "notifications.png";
            }
        }

        private void OnFavoritesClicked(object sender, EventArgs e)
        {
            _showFavoritesOnly = !_showFavoritesOnly;
            _isTodayMode = false; // Сбрасываем режим "только сегодня" при переключении избранного
            RefreshTasks();
        }

        private void OnTasksLabelClicked(object sender, EventArgs e)
        {
            // Сбрасываем фильтры и возвращаемся к изначальному списку задач
            _showFavoritesOnly = false;
            _isTodayMode = false;
            RefreshTasks();
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            _isSettingsOpen = !_isSettingsOpen;

            if (_isSettingsOpen)
            {
                // Закрываем уведомления при открытии настроек
                _isNotificationsOpen = false;
                NotificationsPanel.IsVisible = false;
                NotificationsPanel.Opacity = 0;
                NotificationsOverlay.IsVisible = false;
                
                LoadSettingsIntoPanel();

                // Показываем overlay и панель
                if (SettingsOverlay != null)
                {
                    SettingsOverlay.IsVisible = true;
                    SettingsOverlay.Opacity = 0;
                }
                
                SettingsPanel.IsVisible = true;
                SettingsPanel.Opacity = 0;
                SettingsPanel.Scale = 0.8;
                SettingsPanel.TranslationX = 30;
                
                // Плавная анимация появления: затемнение фона + появление панели с масштабированием и сдвигом
                var overlayAnimation = SettingsOverlay?.FadeTo(1, 300, Easing.CubicOut) ?? Task.CompletedTask;
                var panelFadeAnimation = SettingsPanel.FadeTo(1, 300, Easing.CubicOut);
                var panelScaleAnimation = SettingsPanel.ScaleTo(1, 300, Easing.CubicOut);
                var panelTranslateAnimation = SettingsPanel.TranslateTo(0, 0, 300, Easing.CubicOut);
                
                await Task.WhenAll(overlayAnimation, panelFadeAnimation, panelScaleAnimation, panelTranslateAnimation);
            }
            else
            {
                // Плавная анимация исчезновения
                var panelFadeAnimation = SettingsPanel.FadeTo(0, 250, Easing.CubicIn);
                var panelScaleAnimation = SettingsPanel.ScaleTo(0.8, 250, Easing.CubicIn);
                var panelTranslateAnimation = SettingsPanel.TranslateTo(30, 0, 250, Easing.CubicIn);
                var overlayAnimation = SettingsOverlay?.FadeTo(0, 250, Easing.CubicIn) ?? Task.CompletedTask;
                
                await Task.WhenAll(panelFadeAnimation, panelScaleAnimation, panelTranslateAnimation, overlayAnimation);
                
                SettingsPanel.IsVisible = false;
                SettingsPanel.Opacity = 0;
                SettingsPanel.Scale = 1;
                SettingsPanel.TranslationX = 0;
                
                if (SettingsOverlay != null)
                {
                    SettingsOverlay.IsVisible = false;
                    SettingsOverlay.Opacity = 0;
                }
            }
        }

        private void OnSettingsOverlayTapped(object sender, EventArgs e)
        {
            // Закрываем панель настроек при клике вне её
            if (_isSettingsOpen)
            {
                OnSettingsClicked(sender, e);
            }
        }

        private void ApplySavedTheme()
        {
            switch (AppSettings.AppTheme)
            {
                case AppSettings.ThemeDark:
                    Application.Current!.UserAppTheme = AppTheme.Dark;
                    break;
                case AppSettings.ThemeLight:
                    Application.Current!.UserAppTheme = AppTheme.Light;
                    break;
                default:
                    Application.Current!.UserAppTheme = AppTheme.Unspecified;
                    break;
            }
        }

        private void LoadSettingsIntoPanel()
        {
            try
            {
                var mode = AppSettings.DefaultDisplayMode;
                SettingsDisplayModePicker.SelectedIndex = mode switch
                {
                    AppSettings.DisplayModeKanban => 1,
                    AppSettings.DisplayModeCalendar => 2,
                    AppSettings.DisplayModeGantt => 3,
                    _ => 0
                };

                var theme = AppSettings.AppTheme;
                SettingsThemePicker.SelectedIndex = theme switch
                {
                    AppSettings.ThemeLight => 1,
                    AppSettings.ThemeDark => 2,
                    _ => 0
                };

                var font = AppSettings.FontFamily;
                SettingsFontPicker.SelectedIndex = font switch
                {
                    "OpenSansRegular" => 1,
                    "OpenSansSemibold" => 2,
                    _ => 0 // system
                };

                var accent = AppSettings.AccentColor;
                SettingsAccentPicker.SelectedIndex = accent switch { "Blue" => 1, "Green" => 2, "Orange" => 3, _ => 0 };

                // Загружаем состояние уведомлений
                var notificationsEnabled = AppSettings.NotificationsEnabled;
                SettingsNotificationsSwitch.IsToggled = notificationsEnabled;
                
                QuietHoursStartEntry.Text = AppSettings.QuietHoursStart;
                QuietHoursEndEntry.Text = AppSettings.QuietHoursEnd;
                
                System.Diagnostics.Debug.WriteLine($"Settings loaded: Notifications={notificationsEnabled} (default: true), QuietHours={AppSettings.QuietHoursStart}-{AppSettings.QuietHoursEnd}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void OnSettingsDisplayModeChanged(object? sender, EventArgs e)
        {
            try
            {
                if (SettingsDisplayModePicker.SelectedIndex < 0) return;
                var mode = SettingsDisplayModePicker.SelectedIndex switch
                {
                    1 => AppSettings.DisplayModeKanban,
                    2 => AppSettings.DisplayModeCalendar,
                    3 => AppSettings.DisplayModeGantt,
                    _ => AppSettings.DisplayModeList
                };
                AppSettings.DefaultDisplayMode = mode;
                _displayMode = mode;
                System.Diagnostics.Debug.WriteLine($"Display mode changed to: {mode}");
                RefreshTasks();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSettingsDisplayModeChanged: {ex.Message}");
            }
        }

        private void OnSettingsThemeChanged(object? sender, EventArgs e)
        {
            try
            {
                if (SettingsThemePicker.SelectedIndex < 0) return;
                var theme = SettingsThemePicker.SelectedIndex switch
                {
                    1 => AppSettings.ThemeLight,
                    2 => AppSettings.ThemeDark,
                    _ => AppSettings.ThemeSystem
                };
                AppSettings.AppTheme = theme;
                System.Diagnostics.Debug.WriteLine($"Theme changed to: {theme}");
                ApplySavedTheme();

                // Обновляем элементы, созданные из кода (карточки, тексты, иконка уведомлений)
                RefreshTasks();
                UpdateNotificationIcon();
                if (_isNotificationsOpen)
                {
                    RefreshNotifications();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSettingsThemeChanged: {ex.Message}");
            }
        }

        private void OnSettingsFontChanged(object? sender, EventArgs e)
        {
            try
            {
                if (SettingsFontPicker.SelectedIndex < 0) return;
                var font = SettingsFontPicker.SelectedIndex switch
                {
                    1 => "OpenSansRegular",
                    2 => "OpenSansSemibold",
                    _ => ""
                };
                AppSettings.FontFamily = font;
                System.Diagnostics.Debug.WriteLine($"Font changed to: {font}");
                AppSettings.ApplyToResources(Application.Current!.Resources);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSettingsFontChanged: {ex.Message}");
            }
        }

        private void OnSettingsAccentChanged(object? sender, EventArgs e)
        {
            try
            {
                if (SettingsAccentPicker.SelectedIndex < 0) return;
                var accent = SettingsAccentPicker.SelectedIndex switch
                {
                    1 => "Blue",
                    2 => "Green",
                    3 => "Orange",
                    _ => "Primary"
                };
                AppSettings.AccentColor = accent;
                System.Diagnostics.Debug.WriteLine($"Accent color changed to: {accent}");
                AppSettings.ApplyToResources(Application.Current!.Resources);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSettingsAccentChanged: {ex.Message}");
            }
        }

        private void OnSettingsNotificationsToggled(object? sender, ToggledEventArgs e)
        {
            try
            {
                var isEnabled = SettingsNotificationsSwitch.IsToggled;
                AppSettings.NotificationsEnabled = isEnabled;
                System.Diagnostics.Debug.WriteLine($"Notifications enabled changed to: {isEnabled}");
                
                // Показываем сообщение пользователю
                if (isEnabled)
                {
                    // Уведомления включены - можно показать краткое сообщение
                    System.Diagnostics.Debug.WriteLine("Уведомления включены");
                }
                else
                {
                    // Уведомления выключены
                    System.Diagnostics.Debug.WriteLine("Уведомления выключены");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSettingsNotificationsToggled: {ex.Message}");
            }
        }

        private void OnQuietHoursChanged(object? sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(QuietHoursStartEntry.Text))
            {
                AppSettings.QuietHoursStart = QuietHoursStartEntry.Text.Trim();
                System.Diagnostics.Debug.WriteLine($"Quiet hours start: {AppSettings.QuietHoursStart}");
            }
            if (!string.IsNullOrWhiteSpace(QuietHoursEndEntry.Text))
            {
                AppSettings.QuietHoursEnd = QuietHoursEndEntry.Text.Trim();
                System.Diagnostics.Debug.WriteLine($"Quiet hours end: {AppSettings.QuietHoursEnd}");
            }
        }

        private async void OnOpenProfileFromSettingsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ProfilePage));
        }

        private async void OnDeleteAccountClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Удалить аккаунт",
                "Все ваши задачи и данные будут удалены. Продолжить?",
                "Да",
                "Нет");

            if (!confirm)
                return;

            var (ok, error) = await AuthService.Instance.DeleteCurrentUserAsync();
            if (!ok)
            {
                await DisplayAlert("Ошибка", error, "OK");
                return;
            }

            // Возвращаемся на экран логина
            Application.Current!.MainPage = new LoginPage();
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            if (_isMenuOpen)
            {
                await CloseMenu();
            }
            else
            {
                await OpenMenu();
            }
        }

        private async Task OpenMenu()
        {
            _isMenuOpen = true;
            MenuOverlay.IsVisible = true;

            // Анимация затемнения фона (opacity от 0 до 0.5)
            await MenuOverlay.FadeTo(0.5, 200);

            // Анимация выдвижения меню
            // Меню будет выдвигаться на всю доступную ширину или максимум 300px
            await SideMenu.TranslateTo(0, 0, 300, Easing.CubicOut);
        }

        private async Task CloseMenu()
        {
            // Получаем ширину меню для правильной анимации
            var menuWidth = SideMenu.Width > 0 ? SideMenu.Width : 250;
            
            // Анимация скрытия меню
            await SideMenu.TranslateTo(-menuWidth, 0, 300, Easing.CubicIn);

            // Анимация убирания затемнения
            await MenuOverlay.FadeTo(0, 200);

            MenuOverlay.IsVisible = false;
            _isMenuOpen = false;
        }

        private async void OnMenuOverlayTapped(object sender, EventArgs e) => await CloseMenu();

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            // Сбрасываем фильтры и возвращаемся к изначальному списку задач
            _showFavoritesOnly = false;
            _isTodayMode = false;
            RefreshTasks();
        }

        private async void OnSortClicked(object sender, EventArgs e) => await ShowSortMenu();

        private async void OnMenuMenuItemTapped(object sender, EventArgs e)
        {
            var stackLayout = sender as StackLayout;
            if (stackLayout != null)
            {
                stackLayout.BackgroundColor = Color.FromArgb("#E0E0E0");
                await Task.Delay(100);
                stackLayout.BackgroundColor = Colors.Transparent;
            }
            await CloseMenu();
        }

        private async void OnRefreshMenuItemTapped(object sender, EventArgs e)
        {
            var stackLayout = sender as StackLayout;
            if (stackLayout != null)
            {
                stackLayout.BackgroundColor = Color.FromArgb("#E0E0E0");
                await Task.Delay(100);
                stackLayout.BackgroundColor = Colors.Transparent;
            }
            await CloseMenu();
            RefreshTasks();
        }

        private async void OnSortMenuItemTapped(object sender, EventArgs e)
        {
            var stackLayout = sender as StackLayout;
            if (stackLayout != null)
            {
                stackLayout.BackgroundColor = Color.FromArgb("#E0E0E0");
                await Task.Delay(100);
                stackLayout.BackgroundColor = Colors.Transparent;
            }
            await CloseMenu();
            await ShowSortMenu();
        }

        private async void OnDisplayListTapped(object sender, EventArgs e)
        {
            _displayMode = "List";
            await CloseMenu();
            RefreshTasks();
        }

        private async void OnDisplayKanbanTapped(object sender, EventArgs e)
        {
            _displayMode = "Kanban";
            await CloseMenu();
            RefreshTasks();
        }

        private async void OnDisplayCalendarTapped(object sender, EventArgs e)
        {
            _displayMode = "Calendar";
            await CloseMenu();
            RefreshTasks();
        }

        private async void OnDisplayGanttTapped(object sender, EventArgs e)
        {
            _displayMode = "Gantt";
            await CloseMenu();
            RefreshTasks();
        }

        private void SortTasksForToday()
        {
            _isTodayMode = true;
            var tasks = TaskService.Instance.GetTasks();
            var today = DateTime.Today;

            // Фильтруем задачи на сегодня
            var todayTasks = tasks.Where(task =>
            {
                if (task.IsDateRange)
                {
                    // Для диапазона проверяем, попадает ли сегодня в диапазон
                    if (task.DueDateFrom.HasValue && task.DueDateTo.HasValue)
                    {
                        return task.DueDateFrom.Value.Date <= today && task.DueDateTo.Value.Date >= today;
                    }
                    else if (task.DueDateFrom.HasValue)
                    {
                        return task.DueDateFrom.Value.Date == today;
                    }
                    else if (task.DueDateTo.HasValue)
                    {
                        return task.DueDateTo.Value.Date == today;
                    }
                }
                else
                {
                    // Для простого режима проверяем дату
                    if (task.DueDate.HasValue)
                    {
                        return task.DueDate.Value.Date == today;
                    }
                }
                return false;
            }).ToList();

            // Применяем текущую сортировку, выполненные — в конец
            var sortedTasks = ApplySorting(todayTasks).OrderBy(t => t.IsCompleted).ToList();

            // Обновляем отображение только задач на сегодня
            TasksContainer.Children.Clear();

            if (sortedTasks.Count == 0)
            {
                NoTasksLabel.Text = "На сегодня задач нет";
                NoTasksLabel.IsVisible = true;
                CreateTaskButton.Text = "+ Создать задачу"; // Reset to default text
                CreateTaskButton.IsVisible = true;
                PlusImageButton.IsVisible = false;
            }
            // Check if all tasks for today are completed
            else if (sortedTasks.All(t => t.IsCompleted))
            {
                NoTasksLabel.Text = "Все задачи на сегодня выполнены ;)";
                NoTasksLabel.IsVisible = true;
                CreateTaskButton.Text = "+ Создать новую задачу";
                CreateTaskButton.IsVisible = true;
                PlusImageButton.IsVisible = false;
            }
            else
            {
                NoTasksLabel.IsVisible = false;
                CreateTaskButton.IsVisible = false;
                PlusImageButton.IsVisible = true;
            }

            // If a message is shown, no tasks to display
            if (NoTasksLabel.IsVisible)
            {
                return;
            }

            foreach (var task in sortedTasks)
            {
                var taskFrame = CreateTaskFrame(task);
                TasksContainer.Children.Add(taskFrame);
            }
        }

        private void OnCreateTaskButtonPressed(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            // Подсвечиваем кнопку при нажатии (активное состояние)
            var (accentLight, accentDark) = AppSettings.GetAccentColors();
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark ||
                         Application.Current?.UserAppTheme == AppTheme.Dark;
            button.BackgroundColor = isDark ? accentDark : accentLight;
            button.TextColor = Colors.White;
        }

        private void OnCreateTaskButtonReleased(object sender, EventArgs e)
        {
            // Оставляем кнопку подсвеченной после отпускания
            // Состояние будет сброшено при возврате на страницу
        }

        private async void OnCreateTaskButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            // Кнопка уже подсвечена в OnCreateTaskButtonPressed
            // Создаем и открываем новое окно
            var createTaskPage = new CreateTaskPage();
            await Navigation.PushAsync(createTaskPage);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Подписываемся на событие отправки уведомления (для обновления иконки)
            TaskNotificationScheduler.NotificationSent += OnNotificationSent;
            
            // Сбрасываем состояние кнопки при возврате на страницу
            if (CreateTaskButton != null)
            {
                CreateTaskButton.BackgroundColor = Color.FromArgb("#F5F5F5");
                CreateTaskButton.TextColor = Color.FromArgb("#333333");
            }

            // Обновляем список задач
            RefreshTasks();
            
            // Обновляем иконку уведомлений
            UpdateNotificationIcon();
            
            // Обновляем уведомления, если панель открыта
            if (_isNotificationsOpen)
            {
                RefreshNotifications();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Отписываемся от событий
            TaskNotificationScheduler.NotificationSent -= OnNotificationSent;
        }

        private void OnNotificationSent(object? sender, EventArgs e) => UpdateNotificationIcon();

        private void RefreshTasks()
        {
            var tasks = TaskService.Instance.GetTasks();

            // Обновляем иконку уведомлений
            UpdateNotificationIcon();

            // Если включен режим "только сегодня", фильтруем задачи
            if (_isTodayMode)
            {
                var today = DateTime.Today;
                tasks = tasks.Where(task =>
                {
                    if (task.IsDateRange)
                    {
                        // Для диапазона проверяем, попадает ли сегодня в диапазон
                        if (task.DueDateFrom.HasValue && task.DueDateTo.HasValue)
                        {
                            return task.DueDateFrom.Value.Date <= today && task.DueDateTo.Value.Date >= today;
                        }
                        else if (task.DueDateFrom.HasValue)
                        {
                            return task.DueDateFrom.Value.Date == today;
                        }
                        else if (task.DueDateTo.HasValue)
                        {
                            return task.DueDateTo.Value.Date == today;
                        }
                    }
                    else
                    {
                        // Для простого режима проверяем дату
                        if (task.DueDate.HasValue)
                        {
                            return task.DueDate.Value.Date == today;
                        }
                    }
                    return false;
                }).ToList();
            }

            if (_showFavoritesOnly)
            {
                tasks = tasks.Where(t => t.IsFavorite).ToList();
            }

            TasksContainer.Children.Clear(); // Clear container initially

            if (tasks.Count == 0)
            {
                if (_isTodayMode)
                {
                    NoTasksLabel.Text = "На сегодня задач нет";
                }
                else if (_showFavoritesOnly)
                {
                    NoTasksLabel.Text = "В избранном нет задач";
                }
                else
                {
                    NoTasksLabel.Text = "Похоже, у вас нет задач :(";
                }
                NoTasksLabel.IsVisible = true;
                CreateTaskButton.Text = "+ Создать задачу";
                CreateTaskButton.IsVisible = !_showFavoritesOnly && !_isTodayMode;
                PlusImageButton.IsVisible = false;
                return;
            }

            var sortedTasks = ApplySorting(tasks).OrderBy(t => t.IsCompleted).ToList();

            bool allTasksCompleted = !_showFavoritesOnly && sortedTasks.All(t => t.IsCompleted);

            if (allTasksCompleted)
            {
                NoTasksLabel.Text = "Все задачи выполнены ;)";
                NoTasksLabel.IsVisible = true;
                CreateTaskButton.Text = "+ Создать новую задачу";
                CreateTaskButton.IsVisible = true;
                PlusImageButton.IsVisible = false;
                return;
            }

            // Default state when there are tasks (and not all completed, or in favorites mode)
            NoTasksLabel.IsVisible = false;
            CreateTaskButton.Text = "+ Создать задачу";
            CreateTaskButton.IsVisible = false;
            PlusImageButton.IsVisible = true;

            // Рендерим задачи в зависимости от режима отображения
            switch (_displayMode)
            {
                case "Kanban":
                    RenderKanbanView(sortedTasks);
                    break;
                case "Calendar":
                    RenderCalendarView(sortedTasks);
                    break;
                case "Gantt":
                    RenderGanttView(sortedTasks);
                    break;
                default:
                    foreach (var task in sortedTasks)
                    {
                        var taskFrame = CreateTaskFrame(task);
                        TasksContainer.Children.Add(taskFrame);
                    }
                    break;
            }
        }


        private void RenderKanbanView(List<TaskItem> tasks)
        {
            var isDark = IsDarkTheme();
            var primary = TextPrimary(isDark);

            var todoTasks = tasks.Where(t => !t.IsCompleted).ToList();
            var doneTasks = tasks.Where(t => t.IsCompleted).ToList();
            var kanbanLayout = new Grid { ColumnSpacing = 15 };
            kanbanLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kanbanLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var todoColumn = new StackLayout { Spacing = 10 };
            var doneColumn = new StackLayout { Spacing = 10 };
            todoColumn.Children.Add(new Label { Text = "К выполнению", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = primary });
            doneColumn.Children.Add(new Label { Text = "Выполнено", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = primary });
            foreach (var task in todoTasks)
                todoColumn.Children.Add(CreateTaskFrame(task));
            foreach (var task in doneTasks)
                doneColumn.Children.Add(CreateTaskFrame(task));
            kanbanLayout.Children.Add(todoColumn); Grid.SetColumn(todoColumn, 0);
            kanbanLayout.Children.Add(doneColumn); Grid.SetColumn(doneColumn, 1);
            TasksContainer.Children.Add(kanbanLayout);
        }

        private void RenderCalendarView(List<TaskItem> tasks)
        {
            var isDark = IsDarkTheme();
            var primary = TextPrimary(isDark);
            var secondary = TextSecondary(isDark);

            var weekStart = DateTime.Today;
            while (weekStart.DayOfWeek != DayOfWeek.Monday)
                weekStart = weekStart.AddDays(-1);
            var calendarLayout = new StackLayout { Spacing = 15 };
            var headerLabel = new Label { Text = $"Неделя {weekStart:dd.MM} - {weekStart.AddDays(6):dd.MM}", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = primary };
            calendarLayout.Children.Add(headerLabel);
            for (int i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                var dayTasks = tasks.Where(t => GetTaskDueDateTime(t)?.Date == day).ToList();
                var dayLayout = new StackLayout { Spacing = 5 };
                dayLayout.Children.Add(new Label { Text = $"{day:dddd, dd.MM}", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = primary });
                foreach (var task in dayTasks)
                    dayLayout.Children.Add(CreateTaskFrame(task));
                if (dayTasks.Count == 0)
                    dayLayout.Children.Add(new Label { Text = "Нет задач", FontSize = 12, TextColor = secondary });
                calendarLayout.Children.Add(dayLayout);
            }
            TasksContainer.Children.Add(calendarLayout);
        }

        private void RenderGanttView(List<TaskItem> tasks)
        {
            var isDark = IsDarkTheme();
            var primary = TextPrimary(isDark);
            var secondary = TextSecondary(isDark);

            var ganttLayout = new StackLayout { Spacing = 10 };
            var minDate = tasks.Select(t => GetTaskDueDateTime(t)).Where(d => d.HasValue).Select(d => d!.Value.Date).DefaultIfEmpty(DateTime.Today).Min();
            var maxDate = tasks.Select(t => GetTaskDueDateTime(t)).Where(d => d.HasValue).Select(d => d!.Value.Date).DefaultIfEmpty(DateTime.Today.AddDays(7)).Max();
            ganttLayout.Children.Add(new Label { Text = $"Период: {minDate:dd.MM} - {maxDate:dd.MM}", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = primary });
            foreach (var task in tasks)
            {
                var due = GetTaskDueDateTime(task);
                if (due.HasValue)
                {
                    var taskRow = new StackLayout { Orientation = StackOrientation.Horizontal, Spacing = 10 };
                    taskRow.Children.Add(new Label { Text = task.Title, WidthRequest = 150, FontSize = 14, TextColor = primary });
                    var barFrame = new Frame
                    {
                        BackgroundColor = GetImportanceBgColor(task.Importance),
                        BorderColor = GetImportanceColor(task.Importance),
                        CornerRadius = 4,
                        Padding = 5,
                        HasShadow = false,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };
                    barFrame.Content = new Label { Text = due.Value.ToString("dd.MM HH:mm"), FontSize = 12, TextColor = secondary };
                    taskRow.Children.Add(barFrame);
                    ganttLayout.Children.Add(taskRow);
                }
            }
            TasksContainer.Children.Add(ganttLayout);
        }

        private async void OnPlusImageButtonClicked(object sender, EventArgs e)
        {
            // Открываем CreateTaskPage при нажатии на plus.png
            var createTaskPage = new CreateTaskPage();
            await Navigation.PushAsync(createTaskPage);
        }

        private static Color GetImportanceColor(TaskImportance importance) => importance switch
        {
            TaskImportance.High => Color.FromArgb("#FF5252"),
            TaskImportance.Low => Color.FromArgb("#4CAF50"),
            _ => Color.FromArgb("#FFC107")
        };

        private static Color GetImportanceBgColor(TaskImportance importance) => importance switch
        {
            TaskImportance.High => Color.FromArgb("#FFEBEE"),
            TaskImportance.Low => Color.FromArgb("#E8F5E9"),
            _ => Color.FromArgb("#FFF8E1")
        };

        private Frame CreateTaskFrame(TaskItem task)
        {
            var isDark = IsDarkTheme();
            var primary = TextPrimary(isDark);
            var secondary = TextSecondary(isDark);
            var accent = TextAccent(isDark);

            var borderColor = GetImportanceColor(task.Importance);
            var bgColor = TaskCardBgForImportance(task.Importance, isDark);
            
            // Адаптивные размеры в зависимости от размера окна
            var pageWidth = this.Width;
            var padding = pageWidth > 0 && pageWidth < 600 ? 10 : 15;
            var fontSize = pageWidth > 0 && pageWidth < 600 ? 14 : 18;
            var cornerRadius = pageWidth > 0 && pageWidth < 600 ? 8 : 10;
            
            var frame = new Frame
            {
                BackgroundColor = task.IsCompleted ? TaskCardBgCompleted(isDark) : bgColor,
                BorderColor = borderColor,
                CornerRadius = cornerRadius,
                Padding = padding,
                Margin = new Thickness(0, 0, 0, pageWidth > 0 && pageWidth < 600 ? 8 : 10),
                HasShadow = true,
                HorizontalOptions = LayoutOptions.Fill
            };

            var mainLayout = new StackLayout { Spacing = 10 };

            // Заголовок задачи
            var titleLabel = new Label
            {
                Text = task.Title,
                FontSize = fontSize,
                FontAttributes = FontAttributes.Bold,
                TextColor = task.IsCompleted ? secondary : primary,
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : TextDecorations.None
            };
            mainLayout.Children.Add(titleLabel);

            // Описание задачи
            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                var descFontSize = pageWidth > 0 && pageWidth < 600 ? 12 : 14;
                var descriptionLabel = new Label
                {
                    Text = task.Description,
                    FontSize = descFontSize,
                    TextColor = secondary,
                    LineBreakMode = LineBreakMode.WordWrap,
                    TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : TextDecorations.None
                };
                mainLayout.Children.Add(descriptionLabel);
            }

            // Дата и время
            if (task.IsDateRange)
            {
                // Режим диапазона
                if (task.DueDateFrom.HasValue || task.DueTimeFrom.HasValue || 
                    task.DueDateTo.HasValue || task.DueTimeTo.HasValue)
                {
                    var dateFontSize = pageWidth > 0 && pageWidth < 600 ? 12 : 14;
                    var rangeLabel = new Label
                    {
                        FontSize = dateFontSize,
                        TextColor = accent,
                        LineBreakMode = LineBreakMode.WordWrap
                    };

                    string fromPart = "";
                    if (task.DueTimeFrom.HasValue && task.DueDateFrom.HasValue)
                    {
                        fromPart = $"{task.DueTimeFrom.Value:hh\\:mm} {task.DueDateFrom.Value:dd/MM/yy}";
                    }
                    else if (task.DueTimeFrom.HasValue)
                    {
                        fromPart = $"{task.DueTimeFrom.Value:hh\\:mm}";
                    }
                    else if (task.DueDateFrom.HasValue)
                    {
                        fromPart = $"{task.DueDateFrom.Value:dd/MM/yy}";
                    }

                    string toPart = "";
                    if (task.DueTimeTo.HasValue && task.DueDateTo.HasValue)
                    {
                        toPart = $"{task.DueTimeTo.Value:hh\\:mm} {task.DueDateTo.Value:dd/MM/yy}";
                    }
                    else if (task.DueTimeTo.HasValue)
                    {
                        toPart = $"{task.DueTimeTo.Value:hh\\:mm}";
                    }
                    else if (task.DueDateTo.HasValue)
                    {
                        toPart = $"{task.DueDateTo.Value:dd/MM/yy}";
                    }

                    if (!string.IsNullOrEmpty(fromPart) && !string.IsNullOrEmpty(toPart))
                    {
                        rangeLabel.Text = $"🕐 {fromPart} - {toPart}";
                    }
                    else if (!string.IsNullOrEmpty(fromPart))
                    {
                        rangeLabel.Text = $"🕐 {fromPart}";
                    }
                    else if (!string.IsNullOrEmpty(toPart))
                    {
                        rangeLabel.Text = $"🕐 {toPart}";
                    }

                    if (!string.IsNullOrEmpty(rangeLabel.Text))
                    {
                        mainLayout.Children.Add(rangeLabel);
                    }
                }
            }
            else if (task.DueDate.HasValue || task.DueTime.HasValue)
            {
                // Простой режим
                var dateTimeLayout = new StackLayout 
                { 
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 10
                };

                var dateFontSize = pageWidth > 0 && pageWidth < 600 ? 12 : 14;
                if (task.DueDate.HasValue)
                {
                    var dateLabel = new Label
                    {
                        Text = $"📅 {task.DueDate.Value:dd.MM.yyyy}",
                        FontSize = dateFontSize,
                        TextColor = accent
                    };
                    dateTimeLayout.Children.Add(dateLabel);
                }

                if (task.DueTime.HasValue)
                {
                    var timeLabel = new Label
                    {
                        Text = $"🕐 {task.DueTime.Value:hh\\:mm}",
                        FontSize = dateFontSize,
                        TextColor = accent
                    };
                    dateTimeLayout.Children.Add(timeLabel);
                }

                mainLayout.Children.Add(dateTimeLayout);
            }

            // Повторяемость
            if (task.IsRecurring && task.Recurrence != RecurrenceType.None)
            {
                var recurText = task.Recurrence switch
                {
                    RecurrenceType.Daily => "🔄 Ежедневно",
                    RecurrenceType.Weekly => "🔄 Еженедельно",
                    RecurrenceType.Monthly => "🔄 Ежемесячно",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(recurText))
                {
                    mainLayout.Children.Add(new Label { Text = recurText, FontSize = 12, TextColor = accent });
                }
            }

            // Подзадачи
            if (task.SubTasks != null && task.SubTasks.Count > 0)
            {
                var subTasksHeader = new Label
                {
                    Text = "Подзадачи:",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = secondary,
                    Margin = new Thickness(0, 5, 0, 2)
                };
                mainLayout.Children.Add(subTasksHeader);

                foreach (var subTask in task.SubTasks)
                {
                    var subTaskLayout = new StackLayout { Orientation = StackOrientation.Horizontal, Spacing = 8, Margin = new Thickness(10, 0, 0, 2) };
                    var subTaskCheckLabel = new Label
                    {
                        Text = subTask.IsCompleted ? "☑" : "☐",
                        FontSize = 12,
                        TextColor = primary,
                        VerticalOptions = LayoutOptions.Center
                    };
                    var subTaskLabel = new Label
                    {
                        Text = subTask.Title,
                        FontSize = 12,
                        TextColor = secondary,
                        VerticalOptions = LayoutOptions.Center,
                        TextDecorations = subTask.IsCompleted ? TextDecorations.Strikethrough : TextDecorations.None
                    };
                    subTaskLayout.Children.Add(subTaskCheckLabel);
                    subTaskLayout.Children.Add(subTaskLabel);
                    mainLayout.Children.Add(subTaskLayout);
                }
            }

            // Иконка dot.png для контекстного меню
            var dotImageButton = new ImageButton
            {
                Source = (Application.Current?.RequestedTheme == AppTheme.Dark ||
                          Application.Current?.UserAppTheme == AppTheme.Dark)
                    ? "dotblacktheme.png"
                    : "dot.png",
                WidthRequest = 30,
                HeightRequest = 30,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                BackgroundColor = Colors.Transparent
            };

            dotImageButton.Clicked += async (s, e) =>
            {
                await ShowTaskContextMenu(dotImageButton, task);
            };

            // Чекбокс выполнения
            var checkBox = new Label
            {
                Text = task.IsCompleted ? "☑" : "☐",
                FontSize = 18,
                TextColor = primary,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var checkTap = new TapGestureRecognizer();
            checkTap.Tapped += (s, e) =>
            {
                task.IsCompleted = !task.IsCompleted;
                task.LastUpdated = DateTime.Now;
                TaskService.Instance.UpdateTask(task);
                if (task.IsCompleted && task.Id == _activePomodoroTaskId)
                {
                    _pomodoroCts?.Cancel();
                    _activePomodoroTaskId = null;
                }
                RefreshTasks();
            };
            checkBox.GestureRecognizers.Add(checkTap);

            // Создаем контейнер для чекбокса, заголовка и иконки
            var headerLayout = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            headerLayout.Children.Add(checkBox);
            Grid.SetColumn(checkBox, 0);
            headerLayout.Children.Add(titleLabel);
            Grid.SetColumn(titleLabel, 1);
            headerLayout.Children.Add(dotImageButton);
            Grid.SetColumn(dotImageButton, 2);

            // Заменяем заголовок на headerLayout
            mainLayout.Children.Remove(titleLabel);
            mainLayout.Children.Insert(0, headerLayout);

            // Помодоро таймер на карточке задачи
            if (task.PomodoroDurationMinutes > 0 && !task.IsCompleted)
            {
                var pomodoroSection = new StackLayout { Spacing = 8, Margin = new Thickness(0, 10, 0, 0) };
                var pomodoroStartButton = new Button
                {
                    Text = "⏱ Запустить таймер",
                    BackgroundColor = Color.FromArgb("#4CAF50"),
                    TextColor = Colors.White,
                    CornerRadius = 8,
                    Padding = new Thickness(12, 8)
                };
                var pomodoroTimerLabel = new Label { Text = "25:00", FontSize = 24, FontAttributes = FontAttributes.Bold, IsVisible = false };
                pomodoroTimerLabel.TextColor = primary;
                var pomodoroStopButton = new Button { Text = "Остановить", BackgroundColor = Color.FromArgb("#FF5252"), TextColor = Colors.White, CornerRadius = 8, IsVisible = false };
                pomodoroSection.Children.Add(pomodoroStartButton);
                pomodoroSection.Children.Add(pomodoroTimerLabel);
                pomodoroSection.Children.Add(pomodoroStopButton);
                mainLayout.Children.Add(pomodoroSection);

                pomodoroStartButton.Clicked += async (_, _) =>
                {
                    pomodoroStartButton.IsVisible = false;
                    pomodoroTimerLabel.IsVisible = true;
                    pomodoroStopButton.IsVisible = true;
                    pomodoroTimerLabel.Text = $"{task.PomodoroDurationMinutes:00}:00";
                    _activePomodoroTaskId = task.Id;
                    _pomodoroCts?.Cancel();
                    _pomodoroCts = new System.Threading.CancellationTokenSource();
                    var duration = TimeSpan.FromMinutes(task.PomodoroDurationMinutes);
                    var endTime = DateTime.Now + duration;
                    try
                    {
                        while (DateTime.Now < endTime && !_pomodoroCts.Token.IsCancellationRequested)
                        {
                            var remaining = endTime - DateTime.Now;
                            pomodoroTimerLabel.Text = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
                            await Task.Delay(1000, _pomodoroCts.Token);
                        }
                        if (!_pomodoroCts.Token.IsCancellationRequested)
                            pomodoroTimerLabel.Text = "Готово!";
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        if (task.Id == _activePomodoroTaskId)
                            _activePomodoroTaskId = null;
                        pomodoroStartButton.IsVisible = true;
                        pomodoroTimerLabel.IsVisible = false;
                        pomodoroStopButton.IsVisible = false;
                        RefreshTasks();
                    }
                };
                pomodoroStopButton.Clicked += (_, _) =>
                {
                    _pomodoroCts?.Cancel();
                    if (task.Id == _activePomodoroTaskId)
                        _activePomodoroTaskId = null;
                    RefreshTasks();
                };
            }

            frame.Content = mainLayout;
            return frame;
        }

        private async Task ShowTaskContextMenu(ImageButton sender, TaskItem task)
        {
            string favoriteAction = task.IsFavorite ? "Убрать из избранного" : "В избранное";
            var actions = new string[] { "Редактировать", favoriteAction, "Удалить" };
            string action = await DisplayActionSheet("Действия с задачей", "Отмена", null, actions);

            switch (action)
            {
                case "Редактировать":
                    var editPage = new CreateTaskPage(task);
                    await Navigation.PushAsync(editPage);
                    break;

                case "В избранное":
                    task.IsFavorite = true;
                    TaskService.Instance.UpdateTask(task);
                    RefreshTasks();
                    break;

                case "Убрать из избранного":
                    task.IsFavorite = false;
                    TaskService.Instance.UpdateTask(task);
                    RefreshTasks();
                    break;

                case "Удалить":
                    bool confirm = await DisplayAlert(
                        "Удалить задачу",
                        $"Вы уверены, что хотите удалить задачу \"{task.Title}\"?",
                        "Да",
                        "Нет");

                    if (confirm)
                    {
                        TaskService.Instance.DeleteTask(task.Id);
                        RefreshTasks();
                    }
                    break;
            }
        }


        private async Task ShowSortMenu()
        {
            string action = await DisplayActionSheet(
                "Сортировать по",
                "Отмена",
                null,
                "Пользовательский порядок",
                "Дате завершения",
                "Алфавиту",
                "Последнее обновление");

            switch (action)
            {
                case "Custom order":
                    _currentSortOption = "Пользовательский порядок";
                    RefreshTasks();
                    break;

                case "Due date":
                    _currentSortOption = "Дате завершения";
                    RefreshTasks();
                    break;

                case "Алфавиту":
                    _currentSortOption = "Алфавиту";
                    RefreshTasks();
                    break;

                case "Последнее обновление":
                    _currentSortOption = "Последнее обновление";
                    RefreshTasks();
                    break;
            }
        }

        private List<TaskItem> ApplySorting(List<TaskItem> tasks)
        {
            switch (_currentSortOption)
            {
                case "Custom order":
                    // Порядок по умолчанию - как они были созданы (по CreatedAt)
                    return tasks.OrderBy(t => t.CreatedAt).ToList();

                case "Due date":
                    // Сортировка по дате выполнения
                    return tasks.OrderBy(task =>
                    {
                        if (task.IsDateRange)
                        {
                            if (task.DueDateFrom.HasValue)
                            {
                                return task.DueDateFrom.Value;
                            }
                            if (task.DueDateTo.HasValue)
                            {
                                return task.DueDateTo.Value;
                            }
                        }
                        else
                        {
                            if (task.DueDate.HasValue)
                            {
                                return task.DueDate.Value;
                            }
                        }
                        return DateTime.MaxValue;
                    }).ToList();

                case "Alphabetical":
                case "Алфавиту":
                    return tasks.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ToList();

                case "Last updated":
                case "Последнее обновление":
                    return tasks.OrderByDescending(t => t.LastUpdated).ToList();

                default:
                    return tasks;
            }
        }
    }
}
