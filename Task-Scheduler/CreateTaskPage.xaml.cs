using Task_Scheduler.Models;
using Task_Scheduler.Services;

namespace Task_Scheduler
{
    public partial class CreateTaskPage : ContentPage
    {
        private TaskItem? _editingTask;
        private List<SubTask> _subTasks = new List<SubTask>();

        public CreateTaskPage(TaskItem? task = null)
        {
            InitializeComponent();
            _editingTask = task;

            // Устанавливаем минимальную дату на сегодня для всех DatePicker
            DateTime today = DateTime.Today;
            TaskDatePicker.MinimumDate = today;
            TaskDateFromPicker.MinimumDate = today;
            TaskDateToPicker.MinimumDate = today;

            // Устанавливаем начальный режим (0=Без даты, 1=Одна дата, 2=Диапазон)
            TimeTypePicker.SelectedIndex = 1;

            // Устанавливаем значения по умолчанию для новых полей
            ReminderPicker.SelectedIndex = 0;
            RecurrencePicker.SelectedIndex = 0;
            ImportancePicker.SelectedIndex = 1; // Средняя
            PomodoroPicker.SelectedIndex = 0;   // Выключено

            if (_editingTask != null)
            {
                Title = "Редактировать задачу";
                TaskTitleEntry.Text = _editingTask.Title;
                TaskDescriptionEditor.Text = _editingTask.Description;

                // Переключатель "Указать дату" — вкл, если у задачи есть дата
                bool hasDate = _editingTask.DueDate.HasValue ||
                    _editingTask.DueDateFrom.HasValue ||
                    (_editingTask.IsDateRange && _editingTask.DueDateTo.HasValue);
                UseDateSwitch.IsToggled = hasDate;

                // Устанавливаем режим (0=Без даты, 1=Одна дата, 2=Диапазон)
                if (_editingTask.IsDateRange)
                {
                    TimeTypePicker.SelectedIndex = 2;
                    OnTimeTypeChanged(null, null);
                    
                    if (_editingTask.DueDateFrom.HasValue)
                    {
                        TaskDateFromPicker.Date = _editingTask.DueDateFrom.Value;
                    }
                    
                    if (_editingTask.DueTimeFrom.HasValue)
                    {
                        TaskTimeFromPicker.Time = _editingTask.DueTimeFrom.Value;
                    }
                    
                    if (_editingTask.DueDateTo.HasValue)
                    {
                        TaskDateToPicker.Date = _editingTask.DueDateTo.Value;
                    }
                    
                    if (_editingTask.DueTimeTo.HasValue)
                    {
                        TaskTimeToPicker.Time = _editingTask.DueTimeTo.Value;
                    }
                }
                else
                {
                    if (_editingTask.DueDate.HasValue && _editingTask.DueTime.HasValue)
                    {
                        TimeTypePicker.SelectedIndex = 1;
                        OnTimeTypeChanged(null, null);
                        TaskDatePicker.Date = _editingTask.DueDate.Value;
                        TaskTimePicker.Time = _editingTask.DueTime.Value;
                        TaskTimeEntry.Text = _editingTask.DueTime.Value.ToString(@"hh\:mm");
                    }
                    else if (_editingTask.DueDate.HasValue)
                    {
                        TimeTypePicker.SelectedIndex = 1;
                        OnTimeTypeChanged(null, null);
                        TaskDatePicker.Date = _editingTask.DueDate.Value;
                    }
                    else
                    {
                        TimeTypePicker.SelectedIndex = 0;
                        OnTimeTypeChanged(null, null);
                    }
                }

                _subTasks = new List<SubTask>(_editingTask.SubTasks);
                RefreshSubTasksUI();

                // Загружаем настройки
                ReminderPicker.SelectedIndex = _editingTask.ReminderMinutes switch { 5 => 1, 15 => 2, 30 => 3, _ => 0 };
                RecurrencePicker.SelectedIndex = (int)_editingTask.Recurrence;
                ImportancePicker.SelectedIndex = (int)_editingTask.Importance;
                PomodoroPicker.SelectedIndex = _editingTask.PomodoroDurationMinutes switch { 15 => 1, 25 => 2, 45 => 3, 60 => 4, _ => 0 };
            }
            else
            {
                // Новая задача: по умолчанию без даты
                UseDateSwitch.IsToggled = false;
            }

            UpdateDateSectionVisibility();
        }

        private void OnUseDateToggled(object? sender, ToggledEventArgs e)
        {
            UpdateDateSectionVisibility();
        }

        private void UpdateDateSectionVisibility()
        {
            bool useDate = UseDateSwitch.IsToggled;
            TimeTypeLabel.IsVisible = useDate;
            TimeTypePicker.IsVisible = useDate;
            if (useDate)
                OnTimeTypeChanged(null, null);
            else
            {
                SimpleDateLabel.IsVisible = false;
                TaskDatePicker.IsVisible = false;
                SimpleTimeLabel.IsVisible = false;
                SimpleTimeGrid.IsVisible = false;
                RangeFromDateLabel.IsVisible = false;
                TaskDateFromPicker.IsVisible = false;
                RangeFromTimeLabel.IsVisible = false;
                RangeFromTimeGrid.IsVisible = false;
                RangeToDateLabel.IsVisible = false;
                TaskDateToPicker.IsVisible = false;
                RangeToTimeLabel.IsVisible = false;
                RangeToTimeGrid.IsVisible = false;
            }
        }

        private void OnTimeTypeChanged(object? sender, EventArgs? e)
        {
            bool noDate = TimeTypePicker.SelectedIndex == 0;
            bool isRangeMode = TimeTypePicker.SelectedIndex == 2;

            // Без даты - скрываем всё
            SimpleDateLabel.IsVisible = !noDate && !isRangeMode;
            TaskDatePicker.IsVisible = !noDate && !isRangeMode;
            SimpleTimeLabel.IsVisible = !noDate && !isRangeMode;
            SimpleTimeGrid.IsVisible = !noDate && !isRangeMode;
            TaskTimeEntry.IsVisible = !noDate && !isRangeMode;
            TaskTimePicker.IsVisible = !noDate && !isRangeMode;

            // Режим диапазона
            RangeFromDateLabel.IsVisible = isRangeMode;
            TaskDateFromPicker.IsVisible = isRangeMode;
            RangeFromTimeLabel.IsVisible = isRangeMode;
            RangeFromTimeGrid.IsVisible = isRangeMode;
            RangeToDateLabel.IsVisible = isRangeMode;
            TaskDateToPicker.IsVisible = isRangeMode;
            RangeToTimeLabel.IsVisible = isRangeMode;
            RangeToTimeGrid.IsVisible = isRangeMode;
        }

        private void OnTaskTimeEntryChanged(object? sender, TextChangedEventArgs e)
        {
            if (TimeSpan.TryParseExact(e.NewTextValue ?? "", @"h\:m", null, out var ts))
                TaskTimePicker.Time = ts;
        }
        private void OnTaskTimePickerUnfocused(object? sender, FocusEventArgs e)
        => TaskTimeEntry.Text = $"{TaskTimePicker.Time:hh\\:mm}";

        private void OnTaskTimeFromEntryChanged(object? sender, TextChangedEventArgs e)
        {
            if (TimeSpan.TryParseExact(e.NewTextValue ?? "", @"h\:m", null, out var ts))
                TaskTimeFromPicker.Time = ts;
        }
        private void OnTaskTimeFromPickerUnfocused(object? sender, FocusEventArgs e)
    => TaskTimeFromEntry.Text = $"{TaskTimeFromPicker.Time:hh\\:mm}";


        private void OnTaskTimeToEntryChanged(object? sender, TextChangedEventArgs e)
        {
            if (TimeSpan.TryParseExact(e.NewTextValue ?? "", @"h\:m", null, out var ts))
                TaskTimeToPicker.Time = ts;
        }
        private void OnTaskTimeToPickerUnfocused(object? sender, FocusEventArgs e) => TaskTimeToEntry.Text = $"{TaskTimeToPicker.Time:hh\\:mm}";

        private async void OnAddSubTaskClicked(object sender, EventArgs e)
        {
            string subTaskTitle = await DisplayPromptAsync(
                "Добавить подзадачу", 
                "Введите название подзадачи:", 
                "Добавить", 
                "Отмена", 
                "", 
                -1, 
                Keyboard.Default, 
                "");

            if (!string.IsNullOrWhiteSpace(subTaskTitle))
            {
                var subTask = new SubTask { Title = subTaskTitle };
                _subTasks.Add(subTask);
                RefreshSubTasksUI();
            }
        }

        private void RefreshSubTasksUI()
        {
            SubTasksContainer.Children.Clear();

            foreach (var subTask in _subTasks)
            {
                var subTaskFrame = new Frame
                {
                    BackgroundColor = Colors.White,
                    BorderColor = Colors.LightGray,
                    CornerRadius = 5,
                    Padding = 10,
                    Margin = new Thickness(0, 0, 0, 5),
                    HasShadow = false
                };

                var subTaskLayout = new StackLayout { Orientation = StackOrientation.Horizontal };

                var subTaskLabel = new Label
                {
                    Text = subTask.Title,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.StartAndExpand
                };

                var deleteButton = new Button
                {
                    Text = "✕",
                    BackgroundColor = Colors.Red,
                    TextColor = Colors.White,
                    WidthRequest = 30,
                    HeightRequest = 30,
                    CornerRadius = 15,
                    FontSize = 12,
                    Padding = 0
                };

                var subTaskToDelete = subTask;
                deleteButton.Clicked += (s, e) =>
                {
                    _subTasks.Remove(subTaskToDelete);
                    RefreshSubTasksUI();
                };

                subTaskLayout.Children.Add(subTaskLabel);
                subTaskLayout.Children.Add(deleteButton);
                subTaskFrame.Content = subTaskLayout;
                SubTasksContainer.Children.Add(subTaskFrame);
            }
        }

        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            string title = TaskTitleEntry.Text;
            string description = TaskDescriptionEditor.Text;

            if (string.IsNullOrWhiteSpace(title))
            {
                await DisplayAlert("Ошибка", "Пожалуйста, введите название задачи", "OK");
                return;
            }

            var task = _editingTask ?? new TaskItem();
            task.Title = title;
            task.Description = description;
            task.SubTasks = new List<SubTask>(_subTasks);

            bool noDate = !UseDateSwitch.IsToggled || TimeTypePicker.SelectedIndex == 0;
            bool isRangeMode = UseDateSwitch.IsToggled && TimeTypePicker.SelectedIndex == 2;
            task.IsDateRange = isRangeMode;

            DateTime now = DateTime.Now;

            if (noDate)
            {
                task.DueDate = null;
                task.DueTime = null;
                task.DueDateFrom = null;
                task.DueTimeFrom = null;
                task.DueDateTo = null;
                task.DueTimeTo = null;
            }
            else if (isRangeMode)
            {
                // Режим диапазона
                task.DueDate = null;
                task.DueTime = null;

                if (TaskDateFromPicker.Date != null)
                {
                    task.DueDateFrom = TaskDateFromPicker.Date;
                }

                if (!string.IsNullOrWhiteSpace(TaskTimeFromEntry.Text) && TimeSpan.TryParseExact(TaskTimeFromEntry.Text.Trim(), new[] { @"h\:m", @"hh\:mm" }, null, out var fromTime))
                    task.DueTimeFrom = fromTime;
                else if (TaskTimeFromPicker.Time != default)
                    task.DueTimeFrom = TaskTimeFromPicker.Time;

                if (TaskDateToPicker.Date != null)
                {
                    task.DueDateTo = TaskDateToPicker.Date;
                }

                if (!string.IsNullOrWhiteSpace(TaskTimeToEntry.Text) && TimeSpan.TryParseExact(TaskTimeToEntry.Text.Trim(), new[] { @"h\:m", @"hh\:mm" }, null, out var toTime))
                    task.DueTimeTo = toTime;
                else if (TaskTimeToPicker.Time != default)
                    task.DueTimeTo = TaskTimeToPicker.Time;

                // Проверка даты начала
                if (task.DueDateFrom.HasValue)
                {
                    DateTime startDateTime = task.DueDateFrom.Value.Date + (task.DueTimeFrom ?? TimeSpan.Zero);
                    // Проверяем только если это новая задача или дата была изменена
                    bool dateChanged = _editingTask == null || 
                        !_editingTask.DueDateFrom.HasValue || 
                        _editingTask.DueDateFrom.Value != task.DueDateFrom.Value ||
                        _editingTask.DueTimeFrom != task.DueTimeFrom;
                    
                    if (dateChanged && startDateTime < now)
                    {
                        await DisplayAlert("Ошибка", "Дата и время начала не могут быть в прошлом", "OK");
                        return;
                    }
                }

                // Проверка даты окончания
                if (task.DueDateTo.HasValue)
                {
                    DateTime endDateTime = task.DueDateTo.Value.Date + (task.DueTimeTo ?? TimeSpan.Zero);
                    // Проверяем только если это новая задача или дата была изменена
                    bool dateChanged = _editingTask == null || 
                        !_editingTask.DueDateTo.HasValue || 
                        _editingTask.DueDateTo.Value != task.DueDateTo.Value ||
                        _editingTask.DueTimeTo != task.DueTimeTo;
                    
                    if (dateChanged && endDateTime < now)
                    {
                        await DisplayAlert("Ошибка", "Дата и время окончания не могут быть в прошлом", "OK");
                        return;
                    }
                }
            }
            else
            {
                // Простой режим
                task.DueDateFrom = null;
                task.DueTimeFrom = null;
                task.DueDateTo = null;
                task.DueTimeTo = null;

                if (TaskDatePicker.Date != null)
                {
                    task.DueDate = TaskDatePicker.Date;
                }

                if (!string.IsNullOrWhiteSpace(TaskTimeEntry.Text) && TimeSpan.TryParseExact(TaskTimeEntry.Text.Trim(), new[] { @"h\:m", @"hh\:mm" }, null, out var parsedTime))
                    task.DueTime = parsedTime;
                else if (TaskTimePicker.Time != default)
                    task.DueTime = TaskTimePicker.Time;

                // Проверка даты выполнения (только если задана дата)
                if (task.DueDate.HasValue)
                {
                    DateTime dueDateTime = task.DueDate.Value.Date + (task.DueTime ?? TimeSpan.Zero);
                    // Проверяем только если это новая задача или дата была изменена
                    bool dateChanged = _editingTask == null || 
                        !_editingTask.DueDate.HasValue || 
                        _editingTask.DueDate.Value != task.DueDate.Value ||
                        _editingTask.DueTime != task.DueTime;
                    
                    if (dateChanged && dueDateTime < now)
                    {
                        await DisplayAlert("Ошибка", "Дата и время выполнения не могут быть в прошлом", "OK");
                        return;
                    }
                }
            }

            // Сохраняем настройки напоминания, повторяемости, важности и помодоро
            task.ReminderMinutes = ReminderPicker.SelectedIndex switch { 1 => 5, 2 => 15, 3 => 30, _ => 0 };
            task.Recurrence = (RecurrenceType)RecurrencePicker.SelectedIndex;
            task.IsRecurring = task.Recurrence != RecurrenceType.None;
            task.Importance = (TaskImportance)ImportancePicker.SelectedIndex;
            task.PomodoroDurationMinutes = PomodoroPicker.SelectedIndex switch { 1 => 15, 2 => 25, 3 => 45, 4 => 60, _ => 0 };

            // Сбрасываем флаги уведомления при создании или при изменении даты/времени
            if (_editingTask == null)
            {
                task.DueNotificationSent = false;
                task.ReminderNotificationSent = false;
                task.NotificationDismissed = false;
                TaskService.Instance.AddTask(task);
            }
            else
            {
                var oldDue = GetTaskDueDateTime(_editingTask);
                var newDue = GetTaskDueDateTime(task);
                if (oldDue != newDue)
                {
                    task.DueNotificationSent = false;
                    task.ReminderNotificationSent = false;
                    task.NotificationDismissed = false;
                }
                TaskService.Instance.UpdateTask(task);
            }

            // Закрываем окно и возвращаемся на главную страницу
            await Navigation.PopAsync();
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
    }
}
