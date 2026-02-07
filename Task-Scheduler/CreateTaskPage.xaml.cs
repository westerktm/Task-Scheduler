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

            // Устанавливаем начальный режим
            TimeTypePicker.SelectedIndex = 0;
            OnTimeTypeChanged(null, null);

            if (_editingTask != null)
            {
                Title = "Редактировать задачу";
                TaskTitleEntry.Text = _editingTask.Title;
                TaskDescriptionEditor.Text = _editingTask.Description;
                
                // Устанавливаем режим
                if (_editingTask.IsDateRange)
                {
                    TimeTypePicker.SelectedIndex = 1;
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
                    if (_editingTask.DueDate.HasValue)
                    {
                        TaskDatePicker.Date = _editingTask.DueDate.Value;
                    }
                    
                    if (_editingTask.DueTime.HasValue)
                    {
                        TaskTimePicker.Time = _editingTask.DueTime.Value;
                    }
                }

                _subTasks = new List<SubTask>(_editingTask.SubTasks);
                RefreshSubTasksUI();
            }
        }

        private void OnTimeTypeChanged(object? sender, EventArgs? e)
        {
            bool isRangeMode = TimeTypePicker.SelectedIndex == 1;

            // Простой режим
            SimpleDateLabel.IsVisible = !isRangeMode;
            TaskDatePicker.IsVisible = !isRangeMode;
            SimpleTimeLabel.IsVisible = !isRangeMode;
            TaskTimePicker.IsVisible = !isRangeMode;

            // Режим диапазона
            RangeFromDateLabel.IsVisible = isRangeMode;
            TaskDateFromPicker.IsVisible = isRangeMode;
            RangeFromTimeLabel.IsVisible = isRangeMode;
            TaskTimeFromPicker.IsVisible = isRangeMode;
            RangeToDateLabel.IsVisible = isRangeMode;
            TaskDateToPicker.IsVisible = isRangeMode;
            RangeToTimeLabel.IsVisible = isRangeMode;
            TaskTimeToPicker.IsVisible = isRangeMode;
        }

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

            bool isRangeMode = TimeTypePicker.SelectedIndex == 1;
            task.IsDateRange = isRangeMode;

            DateTime now = DateTime.Now;

            if (isRangeMode)
            {
                // Режим диапазона
                task.DueDate = null;
                task.DueTime = null;

                if (TaskDateFromPicker.Date != null)
                {
                    task.DueDateFrom = TaskDateFromPicker.Date;
                }

                if (TaskTimeFromPicker.Time != null)
                {
                    task.DueTimeFrom = TaskTimeFromPicker.Time;
                }

                if (TaskDateToPicker.Date != null)
                {
                    task.DueDateTo = TaskDateToPicker.Date;
                }

                if (TaskTimeToPicker.Time != null)
                {
                    task.DueTimeTo = TaskTimeToPicker.Time;
                }

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

                if (TaskTimePicker.Time != null)
                {
                    task.DueTime = TaskTimePicker.Time;
                }

                // Проверка даты выполнения
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

            // Сбрасываем флаги уведомления при создании или при изменении даты/времени
            if (_editingTask == null)
            {
                task.DueNotificationSent = false;
                task.NotificationDismissed = false; // Сбрасываем флаг удаления для новой задачи
                TaskService.Instance.AddTask(task);
            }
            else
            {
                var oldDue = GetTaskDueDateTime(_editingTask);
                var newDue = GetTaskDueDateTime(task);
                if (oldDue != newDue)
                {
                    task.DueNotificationSent = false;
                    task.NotificationDismissed = false; // Сбрасываем флаг удаления при изменении даты/времени
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
