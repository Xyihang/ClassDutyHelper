using ClassDutyHelper.Models;
using ClassDutyHelper.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ClassDutyHelper.Views
{
    public partial class TimeSlotDialog : Window
    {
        private readonly DataService _dataService;
        public TimeSlotProject TimeSlot { get; private set; }
        private List<int> _selectedProjectIds = new List<int>();

        public TimeSlotDialog(DataService dataService, TimeSlotProject? timeSlot = null)
        {
            InitializeComponent();
            _dataService = dataService;
            TimeSlot = timeSlot ?? new TimeSlotProject();

            NameTextBox.Text = TimeSlot.Name;
            StartTimeTextBox.Text = TimeSlot.StartTime.ToString(@"hh\:mm");
            EndTimeTextBox.Text = TimeSlot.EndTime.ToString(@"hh\:mm");
            IsEnabledCheckBox.IsChecked = TimeSlot.IsEnabled;

            if (!string.IsNullOrEmpty(TimeSlot.ProjectIds))
            {
                _selectedProjectIds = TimeSlot.ProjectIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();
            }

            LoadProjects();
        }

        private void LoadProjects()
        {
            var projects = _dataService.GetEnabledDutyProjects();
            ProjectsPanel.Children.Clear();

            foreach (var project in projects)
            {
                var checkBox = new CheckBox
                {
                    Content = project.Name,
                    Tag = project.Id,
                    IsChecked = _selectedProjectIds.Contains(project.Id),
                    Margin = new Thickness(0, 5, 0, 5),
                    FontSize = 14
                };
                checkBox.Checked += OnProjectChecked;
                checkBox.Unchecked += OnProjectUnchecked;
                ProjectsPanel.Children.Add(checkBox);
            }
        }

        private void OnProjectChecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is int projectId)
            {
                if (!_selectedProjectIds.Contains(projectId))
                {
                    _selectedProjectIds.Add(projectId);
                }
            }
        }

        private void OnProjectUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is int projectId)
            {
                _selectedProjectIds.Remove(projectId);
            }
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("请输入名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(StartTimeTextBox.Text, out var startTime))
            {
                MessageBox.Show("开始时间格式错误，请使用 HH:mm 格式", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(EndTimeTextBox.Text, out var endTime))
            {
                MessageBox.Show("结束时间格式错误，请使用 HH:mm 格式", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (startTime >= endTime)
            {
                MessageBox.Show("开始时间必须小于结束时间", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TimeSlot.Name = NameTextBox.Text.Trim();
            TimeSlot.StartTime = startTime;
            TimeSlot.EndTime = endTime;
            TimeSlot.ProjectIds = string.Join(",", _selectedProjectIds);
            TimeSlot.IsEnabled = IsEnabledCheckBox.IsChecked == true;

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
