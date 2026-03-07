using ClassDutyHelper.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ClassDutyHelper.Views
{
    public partial class AutoScheduleDialog : Window
    {
        private readonly DataService? _dataService;
        
        public int Mode { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public int CycleDays { get; private set; }
        public bool WeekdaysOnly { get; private set; }
        public bool SkipHolidays { get; private set; }
        public List<DateTime> ExcludeDates { get; private set; } = new List<DateTime>();

        public AutoScheduleDialog()
        {
            InitializeComponent();
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today.AddDays(30);
        }

        public AutoScheduleDialog(DataService dataService) : this()
        {
            _dataService = dataService;
        }

        private void OnPreviewClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            var preview = GeneratePreview();
            PreviewText.Text = preview;
            PreviewBorder.Visibility = Visibility.Visible;
        }

        private bool ValidateInput()
        {
            if (StartDatePicker.SelectedDate == null)
            {
                MessageBox.Show("请选择开始日期", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("请选择结束日期", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(CycleDaysTextBox.Text, out var cycleDays) || cycleDays < 1)
            {
                MessageBox.Show("周期天数必须大于0", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var startDate = StartDatePicker.SelectedDate.Value;
            var endDate = EndDatePicker.SelectedDate.Value;

            if (endDate < startDate)
            {
                MessageBox.Show("结束日期不能早于开始日期", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private string GeneratePreview()
        {
            var startDate = StartDatePicker.SelectedDate!.Value;
            var endDate = EndDatePicker.SelectedDate!.Value;
            var cycleDays = int.Parse(CycleDaysTextBox.Text);
            var weekdaysOnly = WeekdaysOnlyCheckBox.IsChecked == true;
            var skipHolidays = SkipHolidaysCheckBox.IsChecked == true;
            
            ParseExcludeDates();

            var preview = "📅 排班预览：\n\n";
            var scheduleDates = new List<DateTime>();
            var skippedDates = new List<(DateTime date, string reason)>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (weekdaysOnly && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
                {
                    skippedDates.Add((date, "周末"));
                    continue;
                }

                if (skipHolidays && IsHoliday(date))
                {
                    skippedDates.Add((date, "节假日"));
                    continue;
                }

                if (ExcludeDates.Contains(date.Date))
                {
                    skippedDates.Add((date, "自定义排除"));
                    continue;
                }

                scheduleDates.Add(date);
            }

            preview += $"✅ 需要排班的日期：{scheduleDates.Count} 天\n";
            preview += $"⏭️  跳过的日期：{skippedDates.Count} 天\n\n";

            if (skippedDates.Count > 0 && skippedDates.Count <= 10)
            {
                preview += "跳过的日期详情：\n";
                foreach (var (date, reason) in skippedDates.Take(10))
                {
                    preview += $"  • {date:MM-dd} ({GetWeekDayText(date)}) - {reason}\n";
                }
                if (skippedDates.Count > 10)
                {
                    preview += $"  ... 还有 {skippedDates.Count - 10} 天\n";
                }
                preview += "\n";
            }

            if (_dataService != null)
            {
                var students = _dataService.GetAllStudents().Where(s => s.IsEnabled).ToList();
                var projects = _dataService.GetAllDutyProjects().Where(p => p.IsEnabled).ToList();

                preview += $"👥 参与排班的学生：{students.Count} 人\n";
                preview += $"📋 值日项目：{projects.Count} 个\n\n";

                if (students.Count > 0 && projects.Count > 0)
                {
                    preview += "前5天排班预览：\n";
                    foreach (var date in scheduleDates.Take(5))
                    {
                        var dayIndex = scheduleDates.IndexOf(date);
                        var studentIndex = dayIndex % students.Count;
                        var student = students[studentIndex];
                        
                        preview += $"  📌 {date:MM-dd} ({GetWeekDayText(date)})\n";
                        preview += $"     学生：{student.Name}\n";
                        
                        foreach (var project in projects)
                        {
                            var projectStudentIndex = (dayIndex + projects.IndexOf(project)) % students.Count;
                            var projectStudent = students[projectStudentIndex];
                            preview += $"     {project.Name}：{projectStudent.Name}\n";
                        }
                    }
                    
                    if (scheduleDates.Count > 5)
                    {
                        preview += $"\n  ... 还有 {scheduleDates.Count - 5} 天\n";
                    }
                }
            }

            return preview;
        }

        private void ParseExcludeDates()
        {
            ExcludeDates.Clear();
            var excludeText = ExcludeDatesTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(excludeText))
            {
                return;
            }

            var dateStrings = excludeText.Split(new[] { ',', '，', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var dateStr in dateStrings)
            {
                if (DateTime.TryParse(dateStr.Trim(), out var date))
                {
                    ExcludeDates.Add(date.Date);
                }
            }
        }

        private bool IsHoliday(DateTime date)
        {
            var holidays2024 = new List<DateTime>
            {
                new DateTime(2024, 1, 1),
                new DateTime(2024, 2, 10),
                new DateTime(2024, 2, 11),
                new DateTime(2024, 2, 12),
                new DateTime(2024, 2, 13),
                new DateTime(2024, 2, 14),
                new DateTime(2024, 2, 15),
                new DateTime(2024, 2, 16),
                new DateTime(2024, 2, 17),
                new DateTime(2024, 4, 4),
                new DateTime(2024, 4, 5),
                new DateTime(2024, 4, 6),
                new DateTime(2024, 5, 1),
                new DateTime(2024, 5, 2),
                new DateTime(2024, 5, 3),
                new DateTime(2024, 5, 4),
                new DateTime(2024, 5, 5),
                new DateTime(2024, 6, 10),
                new DateTime(2024, 9, 15),
                new DateTime(2024, 9, 16),
                new DateTime(2024, 9, 17),
                new DateTime(2024, 10, 1),
                new DateTime(2024, 10, 2),
                new DateTime(2024, 10, 3),
                new DateTime(2024, 10, 4),
                new DateTime(2024, 10, 5),
                new DateTime(2024, 10, 6),
                new DateTime(2024, 10, 7),
            };

            var holidays2025 = new List<DateTime>
            {
                new DateTime(2025, 1, 1),
                new DateTime(2025, 1, 28),
                new DateTime(2025, 1, 29),
                new DateTime(2025, 1, 30),
                new DateTime(2025, 1, 31),
                new DateTime(2025, 2, 1),
                new DateTime(2025, 2, 2),
                new DateTime(2025, 2, 3),
                new DateTime(2025, 2, 4),
                new DateTime(2025, 4, 4),
                new DateTime(2025, 4, 5),
                new DateTime(2025, 4, 6),
                new DateTime(2025, 5, 1),
                new DateTime(2025, 5, 2),
                new DateTime(2025, 5, 3),
                new DateTime(2025, 5, 4),
                new DateTime(2025, 5, 5),
                new DateTime(2025, 5, 31),
                new DateTime(2025, 6, 1),
                new DateTime(2025, 6, 2),
                new DateTime(2025, 10, 1),
                new DateTime(2025, 10, 2),
                new DateTime(2025, 10, 3),
                new DateTime(2025, 10, 4),
                new DateTime(2025, 10, 5),
                new DateTime(2025, 10, 6),
                new DateTime(2025, 10, 7),
                new DateTime(2025, 10, 8),
            };

            return holidays2024.Contains(date.Date) || holidays2025.Contains(date.Date);
        }

        private string GetWeekDayText(DateTime date)
        {
            var weekdays = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            return weekdays[(int)date.DayOfWeek];
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            Mode = ModeComboBox.SelectedIndex;
            StartDate = StartDatePicker.SelectedDate!.Value;
            EndDate = EndDatePicker.SelectedDate!.Value;
            CycleDays = int.Parse(CycleDaysTextBox.Text);
            WeekdaysOnly = WeekdaysOnlyCheckBox.IsChecked == true;
            SkipHolidays = SkipHolidaysCheckBox.IsChecked == true;
            ParseExcludeDates();

            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
