using Task_Scheduler.Models;
using Task_Scheduler.Services;

namespace Task_Scheduler
{
    public partial class MainPage : ContentPage
    {
        private bool _isMenuOpen = false;
        private bool _isSettingsOpen = false;
        private bool _showFavoritesOnly = false;
        private string _currentSortOption = "Custom order";

        public MainPage()
        {
            InitializeComponent();

            // Инициализируем состояние переключателя темы
            var currentTheme = Application.Current.UserAppTheme;
            if (currentTheme == AppTheme.Unspecified)
            {
                // Следуем за системной темой: считаем, что светлая — выключатель в положении "Off"
                ThemeSwitch.IsToggled = Application.Current.RequestedTheme == AppTheme.Dark;
            }
            else
            {
                ThemeSwitch.IsToggled = currentTheme == AppTheme.Dark;
            }
        }

        private void OnAvatarClicked(object sender, EventArgs e)
        {
            // Обработчик клика по аватару
            // Здесь можно добавить логику, например, открыть меню профиля
        }

        private void OnFavoritesClicked(object sender, EventArgs e)
        {
            _showFavoritesOnly = !_showFavoritesOnly;
            RefreshTasks();
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            _isSettingsOpen = !_isSettingsOpen;
            SettingsPanel.IsVisible = _isSettingsOpen;
        }

        private void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
        {
            // true  -> тёмная тема
            // false -> светлая тема
            Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
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

            // Анимация затемнения фона
            await MenuOverlay.FadeTo(1, 200);

            // Анимация выдвижения меню
            await SideMenu.TranslateTo(0, 0, 300, Easing.CubicOut);
        }

        private async Task CloseMenu()
        {
            // Анимация скрытия меню
            await SideMenu.TranslateTo(-250, 0, 300, Easing.CubicIn);

            // Анимация убирания затемнения
            await MenuOverlay.FadeTo(0, 200);

            MenuOverlay.IsVisible = false;
            _isMenuOpen = false;
        }

        private async void OnMenuOverlayTapped(object sender, EventArgs e)
        {
            await CloseMenu();
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            RefreshTasks();
        }

        private void OnSortClicked(object sender, EventArgs e)
        {
            SortTasksForToday();
        }

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
            SortTasksForToday();
        }

        private void SortTasksForToday()
        {
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
            button.BackgroundColor = Color.FromArgb("#512BD4");
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
            
            // Сбрасываем состояние кнопки при возврате на страницу
            if (CreateTaskButton != null)
            {
                CreateTaskButton.BackgroundColor = Color.FromArgb("#F5F5F5");
                CreateTaskButton.TextColor = Color.FromArgb("#333333");
            }

            // Обновляем список задач
            RefreshTasks();
        }

        private void RefreshTasks()
        {
            var tasks = TaskService.Instance.GetTasks();

            if (_showFavoritesOnly)
            {
                tasks = tasks.Where(t => t.IsFavorite).ToList();
            }

            TasksContainer.Children.Clear(); // Clear container initially

            if (tasks.Count == 0)
            {
                NoTasksLabel.Text = _showFavoritesOnly ? "В избранном нет задач" : "Похоже, у вас нет задач :(";
                NoTasksLabel.IsVisible = true;
                CreateTaskButton.Text = "+ Создать задачу";
                CreateTaskButton.IsVisible = !_showFavoritesOnly;
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

            foreach (var task in sortedTasks)
            {
                var taskFrame = CreateTaskFrame(task);
                TasksContainer.Children.Add(taskFrame);
            }
        }

        private async void OnPlusImageButtonClicked(object sender, EventArgs e)
        {
            // Открываем CreateTaskPage при нажатии на plus.png
            var createTaskPage = new CreateTaskPage();
            await Navigation.PushAsync(createTaskPage);
        }

        private Frame CreateTaskFrame(TaskItem task)
        {
            var frame = new Frame
            {
                BackgroundColor = Colors.White,
                BorderColor = Colors.LightGray,
                CornerRadius = 10,
                Padding = 15,
                Margin = new Thickness(0, 0, 0, 10),
                HasShadow = true
            };

            var mainLayout = new StackLayout { Spacing = 10 };

            // Заголовок задачи
            var titleLabel = new Label
            {
                Text = task.Title,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = task.IsCompleted ? Colors.Gray : Colors.Black,
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : TextDecorations.None
            };
            mainLayout.Children.Add(titleLabel);

            // Описание задачи
            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                var descriptionLabel = new Label
                {
                    Text = task.Description,
                    FontSize = 14,
                    TextColor = Colors.Gray,
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
                    var rangeLabel = new Label
                    {
                        FontSize = 14,
                        TextColor = Colors.DarkBlue,
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

                if (task.DueDate.HasValue)
                {
                    var dateLabel = new Label
                    {
                        Text = $"📅 {task.DueDate.Value:dd.MM.yyyy}",
                        FontSize = 14,
                        TextColor = Colors.DarkBlue
                    };
                    dateTimeLayout.Children.Add(dateLabel);
                }

                if (task.DueTime.HasValue)
                {
                    var timeLabel = new Label
                    {
                        Text = $"🕐 {task.DueTime.Value:hh\\:mm}",
                        FontSize = 14,
                        TextColor = Colors.DarkBlue
                    };
                    dateTimeLayout.Children.Add(timeLabel);
                }

                mainLayout.Children.Add(dateTimeLayout);
            }

            // Подзадачи
            if (task.SubTasks != null && task.SubTasks.Count > 0)
            {
                var subTasksHeader = new Label
                {
                    Text = "Подзадачи:",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.Gray,
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
                        VerticalOptions = LayoutOptions.Center
                    };
                    var subTaskLabel = new Label
                    {
                        Text = subTask.Title,
                        FontSize = 12,
                        TextColor = Colors.Gray,
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
                Source = "dot.png",
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
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var checkTap = new TapGestureRecognizer();
            checkTap.Tapped += (s, e) =>
            {
                task.IsCompleted = !task.IsCompleted;
                task.LastUpdated = DateTime.Now;
                TaskService.Instance.UpdateTask(task);
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

            frame.Content = mainLayout;
            return frame;
        }

        private async Task ShowTaskContextMenu(ImageButton sender, TaskItem task)
        {
            string favoriteAction = task.IsFavorite ? "Убрать из избранного" : "В избранное";
            string completedAction = task.IsCompleted ? "Отменить выполнение" : "Отметить выполненной";
            string action = await DisplayActionSheet(
                "Действия с задачей",
                "Отмена",
                null,
                "Редактировать",
                completedAction,
                favoriteAction,
                "Удалить",
                "Сортировка");

            switch (action)
            {
                case "Редактировать":
                    var editPage = new CreateTaskPage(task);
                    await Navigation.PushAsync(editPage);
                    break;

                case "Отметить выполненной":
                    task.IsCompleted = true;
                    task.LastUpdated = DateTime.Now;
                    TaskService.Instance.UpdateTask(task);
                    RefreshTasks();
                    break;

                case "Отменить выполнение":
                    task.IsCompleted = false;
                    task.LastUpdated = DateTime.Now;
                    TaskService.Instance.UpdateTask(task);
                    RefreshTasks();
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

                case "Сортировка":
                    await ShowSortMenu();
                    break;
            }
        }

        private async Task ShowSortMenu()
        {
            string action = await DisplayActionSheet(
                "Сортировать по",
                "Отмена",
                null,
                "Custom order",
                "Due date",
                "Алфавиту",
                "Последнее обновление");

            switch (action)
            {
                case "☰ Custom order":
                    _currentSortOption = "Custom order";
                    RefreshTasks();
                    break;

                case "📅 Due date":
                    _currentSortOption = "Due date";
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
                    // Сортировка по алфавиту (название задачи)
                    return tasks.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ToList();

                case "Last updated":
                    // Сортировка по дате последнего обновления (новые сначала)
                    return tasks.OrderByDescending(t => t.LastUpdated).ToList();

                default:
                    return tasks;
            }
        }
    }
}
