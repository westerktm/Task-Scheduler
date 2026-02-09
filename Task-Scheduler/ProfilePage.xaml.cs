using Task_Scheduler.Models;
using Task_Scheduler.Services;

namespace Task_Scheduler
{
    public partial class ProfilePage : ContentPage
    {
        private string _currentChartType = "Line"; // Line, Donut, Heatmap, Bar

        public ProfilePage()
        {
            InitializeComponent();
            LoadAvatar();
            _currentChartType = AppSettings.ProfileChartType;
            RenderCurrentChart();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadAvatar();
            RenderCurrentChart();
        }

        private void LoadAvatar()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(AppSettings.AvatarPath) &&
                    File.Exists(AppSettings.AvatarPath))
                {
                    ProfileAvatarImage.Source = ImageSource.FromFile(AppSettings.AvatarPath);
                }
            }
            catch
            {
                // Игнорируем ошибки чтения аватара
            }
        }

        private async void OnEditAvatarProfileClicked(object sender, EventArgs e)
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

        private void OnEditChartsTapped(object? sender, EventArgs e)
        {
            ChartsDropdown.IsVisible = !ChartsDropdown.IsVisible;
        }

        private void OnChartOptionTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is not string type)
                return;

            _currentChartType = type;
            AppSettings.ProfileChartType = type;
            ChartsDropdown.IsVisible = false;
            RenderCurrentChart();
        }

        private void RenderCurrentChart()
        {
            if (TaskChartsContainer == null)
                return;

            TaskChartsContainer.Children.Clear();

            var tasks = TaskService.Instance.GetTasks().ToList();
            switch (_currentChartType)
            {
                case "Donut":
                    RenderDonutChart(tasks);
                    break;
                case "Heatmap":
                    RenderHeatmapChart(tasks);
                    break;
                case "Bar":
                    RenderBarChart(tasks);
                    break;
                default:
                    RenderLineChart(tasks);
                    break;
            }
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

        private static (int done, int overdue, int active) GetTaskStatusCounts(IEnumerable<TaskItem> tasks)
        {
            var now = DateTime.Now;
            int done = 0, overdue = 0, active = 0;

            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    done++;
                    continue;
                }

                var due = GetTaskDueDateTime(task);
                if (due.HasValue && due.Value < now)
                    overdue++;
                else
                    active++;
            }

            return (done, overdue, active);
        }

        private void RenderDonutChart(List<TaskItem> tasks)
        {
            var (done, overdue, active) = GetTaskStatusCounts(tasks);
            int total = done + overdue + active;
            if (total == 0)
            {
                TaskChartsContainer.Children.Add(new Label
                {
                    Text = "Нет данных для диаграммы",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                });
                return;
            }

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Star }
                },
                ColumnSpacing = 4
            };

            Color doneColor = Colors.Green;
            Color overdueColor = Colors.Red;
            Color activeColor = Colors.Orange;

            double max = new[] { done, overdue, active }.Max();
            max = Math.Max(max, 1);

            (int value, string label, Color color)[] segments =
            {
                (done, "Выполнено", doneColor),
                (overdue, "Просрочено", overdueColor),
                (active, "Активно", activeColor)
            };

            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                var stack = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalOptions = LayoutOptions.Fill,
                    Spacing = 4
                };

                var bar = new BoxView
                {
                    HeightRequest = 60 * (seg.value / max),
                    CornerRadius = 8,
                    Color = seg.color,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.End
                };

                var lbl = new Label
                {
                    Text = $"{seg.label}\n{seg.value}",
                    FontSize = 11,
                    HorizontalTextAlignment = TextAlignment.Center
                };

                stack.Children.Add(bar);
                stack.Children.Add(lbl);

                grid.Children.Add(stack);
                Grid.SetColumn(stack, i);
            }

            TaskChartsContainer.Children.Add(grid);
        }

        private void RenderLineChart(List<TaskItem> tasks)
        {
            var now = DateTime.Today;
            var days = Enumerable.Range(0, 7)
                .Select(offset => now.AddDays(-6 + offset))
                .ToList();

            var created = days
                .Select(d => tasks.Count(t => t.CreatedAt.Date == d.Date))
                .ToArray();

            var completed = days
                .Select(d => tasks.Count(t => t.IsCompleted && t.LastUpdated.Date == d.Date))
                .ToArray();

            int max = Math.Max(created.Max(), completed.Max());
            if (max == 0) max = 1;

            var grid = new Grid
            {
                ColumnSpacing = 4
            };
            for (int i = 0; i < days.Count; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var (accentLight, _) = AppSettings.GetAccentColors();

            for (int i = 0; i < days.Count; i++)
            {
                var colLayout = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalOptions = LayoutOptions.Fill,
                    Spacing = 2
                };

                var createdBar = new BoxView
                {
                    HeightRequest = 60 * (created[i] / (double)max),
                    Color = accentLight,
                    Opacity = 0.6,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.End,
                    WidthRequest = 10
                };

                var completedBar = new BoxView
                {
                    HeightRequest = 60 * (completed[i] / (double)max),
                    Color = Colors.LimeGreen,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.End,
                    WidthRequest = 6
                };

                var bars = new Grid { HeightRequest = 70 };
                bars.Children.Add(createdBar);
                bars.Children.Add(completedBar);

                var dayLabel = new Label
                {
                    Text = days[i].ToString("dd.MM"),
                    FontSize = 10,
                    HorizontalTextAlignment = TextAlignment.Center
                };

                colLayout.Children.Add(bars);
                colLayout.Children.Add(dayLabel);

                grid.Children.Add(colLayout);
                Grid.SetColumn(colLayout, i);
            }

            TaskChartsContainer.Children.Add(grid);
        }

        private void RenderHeatmapChart(List<TaskItem> tasks)
        {
            var days = Enumerable.Range(0, 28)
                .Select(offset => DateTime.Today.AddDays(-27 + offset))
                .ToList();

            var counts = days
                .Select(d => tasks.Count(t => t.IsCompleted && t.LastUpdated.Date == d.Date))
                .ToArray();

            int max = counts.Max();
            if (max == 0) max = 1;

            var grid = new Grid
            {
                RowSpacing = 2,
                ColumnSpacing = 2
            };

            for (int c = 0; c < 7; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            for (int r = 0; r < 4; r++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            var (accentLight, _) = AppSettings.GetAccentColors();

            for (int i = 0; i < days.Count; i++)
            {
                int r = i / 7;
                int c = i % 7;

                double intensity = counts[i] / (double)max;
                var color = accentLight.WithAlpha(0.1f + (float)(0.7 * intensity));

                var box = new BoxView
                {
                    Color = color,
                    CornerRadius = 3
                };

                grid.Children.Add(box);
                Grid.SetRow(box, r);
                Grid.SetColumn(box, c);
            }

            TaskChartsContainer.Children.Add(grid);
        }

        private void RenderBarChart(List<TaskItem> tasks)
        {
            int low = tasks.Count(t => t.Importance == TaskImportance.Low);
            int medium = tasks.Count(t => t.Importance == TaskImportance.Medium);
            int high = tasks.Count(t => t.Importance == TaskImportance.High);

            int max = new[] { low, medium, high }.Max();
            if (max == 0) max = 1;

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Star },
                    new ColumnDefinition{ Width = GridLength.Star }
                },
                ColumnSpacing = 6
            };

            (int value, string label, Color color)[] bars =
            {
                (low, "Низкая", Colors.SteelBlue),
                (medium, "Средняя", Colors.Orange),
                (high, "Высокая", Colors.Crimson)
            };

            for (int i = 0; i < bars.Length; i++)
            {
                var b = bars[i];
                var stack = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalOptions = LayoutOptions.Fill,
                    Spacing = 4
                };

                var bar = new BoxView
                {
                    HeightRequest = 60 * (b.value / (double)max),
                    Color = b.color,
                    CornerRadius = 6,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.End,
                    WidthRequest = 18
                };

                var lbl = new Label
                {
                    Text = $"{b.label}\n{b.value}",
                    FontSize = 11,
                    HorizontalTextAlignment = TextAlignment.Center
                };

                stack.Children.Add(bar);
                stack.Children.Add(lbl);

                grid.Children.Add(stack);
                Grid.SetColumn(stack, i);
            }

            TaskChartsContainer.Children.Add(grid);
        }

        private async void OnSaveProfileClicked(object sender, EventArgs e)
        {
            // Здесь можно сохранить данные профиля в сервис/хранилище.
            await DisplayAlert("Профиль", "Изменения профиля сохранены.", "OK");
            await Navigation.PopAsync();
        }
    }
}

