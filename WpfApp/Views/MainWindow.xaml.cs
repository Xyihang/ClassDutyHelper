using ClassDutyHelper.Helpers;
using ClassDutyHelper.Models;
using ClassDutyHelper.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ClassDutyHelper.Views
{
    public partial class MainWindow : Window
    {
        private readonly DataService _dataService;
        private readonly ExcelService _excelService;
        private readonly ReminderService _reminderService;
        private readonly SyncService _syncService;
        private TopBarWindow? _topBarWindow;
        private AppSettings _settings = null!;

        public ObservableCollection<Student> Students { get; set; }
        public ObservableCollection<DutyProject> Projects { get; set; }
        public ObservableCollection<DutyRecord> DutyRecords { get; set; }
        public ObservableCollection<Reminder> Reminders { get; set; }
        public ObservableCollection<TodayDutyItem> TodayDuties { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _dataService = new DataService();
            _excelService = new ExcelService();
            _reminderService = new ReminderService(_dataService);
            _syncService = new SyncService(_dataService);

            Students = new ObservableCollection<Student>();
            Projects = new ObservableCollection<DutyProject>();
            DutyRecords = new ObservableCollection<DutyRecord>();
            Reminders = new ObservableCollection<Reminder>();
            TodayDuties = new ObservableCollection<TodayDutyItem>();

            StudentsDataGrid.ItemsSource = Students;
            ProjectsDataGrid.ItemsSource = Projects;
            ScheduleDataGrid.ItemsSource = DutyRecords;
            RemindersDataGrid.ItemsSource = Reminders;
            TodayDutyList.ItemsSource = TodayDuties;

            LoadSettings();
            LoadData();
            UpdateTodayView();

            _reminderService.ReminderTriggered += OnReminderTriggered;
            _reminderService.WarningTriggered += OnWarningTriggered;

            var settings = _dataService.GetAppSettings();
            if (!string.IsNullOrEmpty(settings.CloudFormUrl) && !string.IsNullOrEmpty(settings.AdminKey))
            {
                _syncService.StartSync(settings.SyncIntervalMinutes);
            }

            ScheduleCalendar.SelectedDate = DateTime.Today;
            ScheduleCalendar.SelectedDatesChanged += (sender, e) => LoadScheduleData();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (_settings.HideMainWindowOnStart)
            {
                Hide();
            }

            if (_settings.TopBarVisible)
            {
                ShowTopBar();
            }
        }

        private void LoadSettings()
        {
            _settings = _dataService.GetAppSettings();
            ClassNameTextBox.Text = _settings.ClassName ?? "";
            ShowTopBarCheckBox.IsChecked = _settings.TopBarVisible;
            TopBarOpacitySlider.Value = _settings.TopBarOpacity * 100;
            TopBarHeightSlider.Value = _settings.TopBarHeight;
            TopBarPositionComboBox.SelectedIndex = _settings.TopBarPosition;
            StartupCheckBox.IsChecked = _settings.StartWithWindows;
            TopMostCheckBox.IsChecked = _settings.WindowTopMost;
            HideOnStartCheckBox.IsChecked = _settings.HideMainWindowOnStart;
            CloudFormUrlTextBox.Text = _settings.CloudFormUrl ?? "";
            AdminKeyTextBox.Text = _settings.AdminKey ?? "";

            LoadTimeSlotData();

            if (_settings.WindowTopMost)
            {
                WindowHelper.SetTopMost(this, true);
            }

            if (_settings.StartWithWindows)
            {
                StartupHelper.SetStartup(true);
            }
        }

        private void LoadData()
        {
            Students.Clear();
            foreach (var student in _dataService.GetAllStudents())
            {
                Students.Add(student);
            }

            Projects.Clear();
            foreach (var project in _dataService.GetAllDutyProjects())
            {
                Projects.Add(project);
            }

            Reminders.Clear();
            foreach (var reminder in _dataService.GetAllReminders())
            {
                Reminders.Add(reminder);
            }
        }

        private void UpdateTodayView()
        {
            var today = DateTime.Today;
            TodayDateText.Text = today.ToString("yyyy年MM月dd日");
            TodayWeekText.Text = GetWeekDayText(today);

            var records = _dataService.GetDutyRecordsByDate(today);
            TodayDuties.Clear();

            if (records.Count == 0)
            {
                NoDutyText.Visibility = Visibility.Visible;
                TodayDutyList.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoDutyText.Visibility = Visibility.Collapsed;
                TodayDutyList.Visibility = Visibility.Visible;

                var grouped = records.GroupBy(r => r.DutyProjectId);
                foreach (var group in grouped)
                {
                    var project = group.First().DutyProject;
                    var students = group.Select(g => g.Student?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList();

                    TodayDuties.Add(new TodayDutyItem
                    {
                        ProjectName = project?.Name ?? "",
                        StudentsText = string.Join("、", students),
                        PersonCountText = $"{students.Count}人"
                    });
                }
            }

            _topBarWindow?.UpdateInfo();
        }

        private string GetWeekDayText(DateTime date)
        {
            var weekdays = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            return weekdays[(int)date.DayOfWeek];
        }

        #region Window Events
        private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void OnPinClick(object sender, RoutedEventArgs e)
        {
            _settings.WindowTopMost = !_settings.WindowTopMost;
            _dataService.UpdateAppSettings(_settings);
            TopMostCheckBox.IsChecked = _settings.WindowTopMost;
            WindowHelper.SetTopMost(this, _settings.WindowTopMost);
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
        #endregion

        #region Navigation
        private void OnNavigationChanged(object sender, RoutedEventArgs e)
        {
            if (TodayView == null) return;
            
            FrameworkElement? targetView = NavToday.IsChecked == true ? TodayView :
                             NavSchedule.IsChecked == true ? ScheduleView :
                             NavStudents.IsChecked == true ? StudentsView :
                             NavProjects.IsChecked == true ? ProjectsView :
                             NavReminders.IsChecked == true ? RemindersView :
                             NavSettings.IsChecked == true ? SettingsView : null;

            if (targetView == null) return;

            var allViews = new FrameworkElement[] { TodayView, ScheduleView, StudentsView, ProjectsView, RemindersView, SettingsView };
            
            foreach (var view in allViews)
            {
                if (view == targetView)
                {
                    view.Visibility = Visibility.Visible;
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200), FillBehavior.Stop);
                    fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                    view.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                }
                else if (view.Visibility == Visibility.Visible)
                {
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150), FillBehavior.Stop);
                    fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
                    fadeOut.Completed += (s, args) => view.Visibility = Visibility.Collapsed;
                    view.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }
            }

            if (NavSchedule.IsChecked == true)
            {
                LoadScheduleData();
            }
        }

        private void LoadScheduleData()
        {
            var date = ScheduleCalendar.SelectedDate ?? DateTime.Today;
            DutyRecords.Clear();
            foreach (var record in _dataService.GetDutyRecordsByDate(date))
            {
                DutyRecords.Add(record);
            }
        }
        #endregion

        #region Student Management
        private void OnAddStudentClick(object sender, RoutedEventArgs e)
        {
            var dialog = new StudentDialog();
            if (dialog.ShowDialog() == true)
            {
                var student = dialog.Student;
                if (_dataService.AddStudent(student))
                {
                    Students.Add(_dataService.GetStudentByName(student.Name)!);
                }
                else
                {
                    MessageBox.Show("该学生已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OnEditStudentClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Student student)
            {
                var dialog = new StudentDialog(student);
                if (dialog.ShowDialog() == true)
                {
                    _dataService.UpdateStudent(dialog.Student);
                    LoadData();
                }
            }
        }

        private void OnDeleteStudentClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Student student)
            {
                if (MessageBox.Show($"确定删除学生 {student.Name}？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _dataService.DeleteStudent(student.Id);
                    Students.Remove(student);
                }
            }
        }

        private void OnImportStudentsClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel文件|*.xlsx;*.xls",
                Title = "导入学生名单"
            };

            if (dialog.ShowDialog() == true)
            {
                var importDialog = new ImportColumnDialog("导入学生名单", new[] { "姓名列", "学号列", "值日组列" });
                if (importDialog.ShowDialog() == true)
                {
                    var cols = importDialog.Columns;
                    var students = _excelService.ImportStudents(dialog.FileName, cols[0], cols[1] > 0 ? cols[1] : null, cols[2] > 0 ? cols[2] : null, out var error);

                    if (!string.IsNullOrEmpty(error))
                    {
                        MessageBox.Show(error, "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    foreach (var student in students)
                    {
                        _dataService.AddStudent(student);
                    }

                    LoadData();
                    MessageBox.Show($"成功导入 {students.Count} 名学生", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void OnExportStudentsClick(object sender, RoutedEventArgs e)
        {
            var path = _excelService.ExportStudents(Students.ToList());
            MessageBox.Show($"名单已导出到：{path}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnClearStudentsClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定清空所有学生名单？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _dataService.DeleteAllStudents();
                Students.Clear();
            }
        }
        #endregion

        #region Project Management
        private void OnAddProjectClick(object sender, RoutedEventArgs e)
        {
            var dialog = new ProjectDialog();
            if (dialog.ShowDialog() == true)
            {
                var project = dialog.Project;
                if (_dataService.AddDutyProject(project))
                {
                    Projects.Add(_dataService.GetDutyProjectByName(project.Name)!);
                }
                else
                {
                    MessageBox.Show("该项目已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OnEditProjectClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DutyProject project)
            {
                var dialog = new ProjectDialog(project);
                if (dialog.ShowDialog() == true)
                {
                    _dataService.UpdateDutyProject(dialog.Project);
                    LoadData();
                }
            }
        }

        private void OnDeleteProjectClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DutyProject project)
            {
                if (MessageBox.Show($"确定删除项目 {project.Name}？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _dataService.DeleteDutyProject(project.Id);
                    Projects.Remove(project);
                }
            }
        }

        private void OnImportProjectsClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel文件|*.xlsx;*.xls",
                Title = "导入值日项目"
            };

            if (dialog.ShowDialog() == true)
            {
                var importDialog = new ImportColumnDialog("导入值日项目", new[] { "项目名称列", "默认人数列" });
                if (importDialog.ShowDialog() == true)
                {
                    var cols = importDialog.Columns;
                    var projects = _excelService.ImportDutyProjects(dialog.FileName, cols[0], cols[1] > 0 ? cols[1] : null, out var error);

                    if (!string.IsNullOrEmpty(error))
                    {
                        MessageBox.Show(error, "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    foreach (var project in projects)
                    {
                        _dataService.AddDutyProject(project);
                    }

                    LoadData();
                    MessageBox.Show($"成功导入 {projects.Count} 个项目", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void OnExportProjectsClick(object sender, RoutedEventArgs e)
        {
            var path = _excelService.ExportDutyProjects(Projects.ToList());
            MessageBox.Show($"项目已导出到：{path}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Schedule Management
        private void OnScheduleDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadScheduleData();
        }

        private void OnAutoScheduleClick(object sender, RoutedEventArgs e)
        {
            var dialog = new AutoScheduleDialog(_dataService);
            if (dialog.ShowDialog() == true)
            {
                var count = _dataService.AutoSchedule(dialog.Mode, dialog.CycleDays, dialog.StartDate, dialog.EndDate, dialog.WeekdaysOnly, dialog.SkipHolidays, dialog.ExcludeDates);
                LoadScheduleData();
                UpdateTodayView();
                MessageBox.Show($"自动排班完成，共生成 {count} 条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnClearScheduleClick(object sender, RoutedEventArgs e)
        {
            var date = ScheduleCalendar.SelectedDate ?? DateTime.Today;
            if (MessageBox.Show($"确定清空 {date:yyyy-MM-dd} 的排班？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _dataService.DeleteDutyRecordsByDate(date);
                LoadScheduleData();
                UpdateTodayView();
            }
        }

        private void OnClearScheduleRangeClick(object sender, RoutedEventArgs e)
        {
            var dialog = new DateRangeDialog();
            if (dialog.ShowDialog() == true)
            {
                if (MessageBox.Show($"确定清空 {dialog.StartDate:yyyy-MM-dd} 到 {dialog.EndDate:yyyy-MM-dd} 的排班？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _dataService.DeleteDutyRecordsByDateRange(dialog.StartDate, dialog.EndDate);
                    LoadScheduleData();
                    UpdateTodayView();
                }
            }
        }

        private void OnClearAllScheduleClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定清空所有排班记录？此操作不可恢复！", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _dataService.ClearAllDutyRecords();
                LoadScheduleData();
                UpdateTodayView();
            }
        }

        private void OnEditScheduleClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DutyRecord record)
            {
                var dialog = new ScheduleDialog(record, _dataService.GetEnabledStudents().ToList(), _dataService.GetEnabledDutyProjects().ToList());
                if (dialog.ShowDialog() == true)
                {
                    _dataService.UpdateDutyRecord(dialog.Record);
                    LoadScheduleData();
                    UpdateTodayView();
                }
            }
        }

        private void OnDeleteScheduleClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DutyRecord record)
            {
                if (MessageBox.Show("确定删除此排班记录？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _dataService.DeleteDutyRecord(record.Id);
                    DutyRecords.Remove(record);
                    UpdateTodayView();
                }
            }
        }

        private void OnExportScheduleClick(object sender, RoutedEventArgs e)
        {
            var dialog = new DateRangeDialog();
            if (dialog.ShowDialog() == true)
            {
                var records = _dataService.GetDutyRecordsByDateRange(dialog.StartDate, dialog.EndDate);
                var path = _excelService.ExportDutyRecords(records, _settings.ClassName ?? "班级");
                MessageBox.Show($"值日表已导出到：{path}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnImportScheduleClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel文件|*.xlsx;*.xls",
                Title = "导入值日表"
            };

            if (dialog.ShowDialog() == true)
            {
                var records = _excelService.ImportDutyRecords(dialog.FileName, out var error);

                if (!string.IsNullOrEmpty(error))
                {
                    MessageBox.Show(error, "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                foreach (var record in records)
                {
                    var project = _dataService.GetDutyProjectByName(record.ProjectName);
                    if (project == null) continue;

                    foreach (var studentName in record.StudentNames)
                    {
                        var student = _dataService.GetStudentByName(studentName);
                        if (student == null) continue;

                        _dataService.AddDutyRecord(new DutyRecord
                        {
                            DutyDate = record.Date,
                            DutyProjectId = project.Id,
                            StudentId = student.Id,
                            Remark = record.Remark
                        });
                    }
                }

                LoadScheduleData();
                UpdateTodayView();
                MessageBox.Show($"成功导入 {records.Count} 条记录", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region Reminder Management
        private void OnAddReminderClick(object sender, RoutedEventArgs e)
        {
            var dialog = new ReminderDialog();
            if (dialog.ShowDialog() == true)
            {
                _dataService.AddReminder(dialog.Reminder);
                LoadData();
            }
        }

        private void OnEditReminderClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Reminder reminder)
            {
                var dialog = new ReminderDialog(reminder);
                if (dialog.ShowDialog() == true)
                {
                    _dataService.UpdateReminder(dialog.Reminder);
                    LoadData();
                }
            }
        }

        private void OnDeleteReminderClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Reminder reminder)
            {
                if (MessageBox.Show($"确定删除提醒 {reminder.Name}？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _dataService.DeleteReminder(reminder.Id);
                    Reminders.Remove(reminder);
                }
            }
        }

        private void OnReminderTriggered(Reminder reminder)
        {
            Dispatcher.Invoke(() =>
            {
            });
        }

        private void OnWarningTriggered(Reminder reminder, int minutes)
        {
            Dispatcher.Invoke(() =>
            {
            });
        }
        #endregion

        #region Settings
        private void OnClassNameChanged(object sender, TextChangedEventArgs e)
        {
            _settings.ClassName = ClassNameTextBox.Text;
            _dataService.UpdateAppSettings(_settings);
        }

        private void OnTopBarSettingChanged(object sender, RoutedEventArgs e)
        {
            _settings.TopBarVisible = ShowTopBarCheckBox.IsChecked == true;
            _dataService.UpdateAppSettings(_settings);

            if (_settings.TopBarVisible)
            {
                ShowTopBar();
            }
            else
            {
                CloseTopBar();
            }
        }

        private void OnTopBarOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_settings == null) return;
            _settings.TopBarOpacity = TopBarOpacitySlider.Value / 100.0;
            _dataService.UpdateAppSettings(_settings);
            _topBarWindow?.SetOpacity(_settings.TopBarOpacity);
        }

        private void OnTopBarHeightChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_settings == null) return;
            _settings.TopBarHeight = (int)TopBarHeightSlider.Value;
            _dataService.UpdateAppSettings(_settings);
            _topBarWindow?.SetHeight(_settings.TopBarHeight);
        }

        private void OnTopBarPositionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null || TopBarPositionComboBox.SelectedItem == null) return;
            var selectedItem = (System.Windows.Controls.ComboBoxItem)TopBarPositionComboBox.SelectedItem;
            _settings.TopBarPosition = int.Parse(selectedItem.Tag.ToString() ?? "0");
            _dataService.UpdateAppSettings(_settings);
            _topBarWindow?.SetPosition(_settings.TopBarPosition);
        }

        private void OnStartupChanged(object sender, RoutedEventArgs e)
        {
            _settings.StartWithWindows = StartupCheckBox.IsChecked == true;
            _dataService.UpdateAppSettings(_settings);
            StartupHelper.SetStartup(_settings.StartWithWindows);
        }

        private void OnTopMostChanged(object sender, RoutedEventArgs e)
        {
            _settings.WindowTopMost = TopMostCheckBox.IsChecked == true;
            _dataService.UpdateAppSettings(_settings);
            WindowHelper.SetTopMost(this, _settings.WindowTopMost);
        }

        private void OnHideOnStartChanged(object sender, RoutedEventArgs e)
        {
            _settings.HideMainWindowOnStart = HideOnStartCheckBox.IsChecked == true;
            _dataService.UpdateAppSettings(_settings);
        }

        private void OnCloudFormUrlChanged(object sender, TextChangedEventArgs e)
        {
            _settings.CloudFormUrl = CloudFormUrlTextBox.Text;
            _dataService.UpdateAppSettings(_settings);
        }

        private void OnAdminKeyChanged(object sender, TextChangedEventArgs e)
        {
            _settings.AdminKey = AdminKeyTextBox.Text;
            _dataService.UpdateAppSettings(_settings);
        }

        private void OnBackupClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "数据库文件|*.db",
                FileName = $"backup_{DateTime.Now:yyyyMMdd_HHmm}.db",
                Title = "备份数据"
            };

            if (dialog.ShowDialog() == true)
            {
                File.Copy(AppDbContext.GetDatabasePath(), dialog.FileName, true);
                MessageBox.Show($"备份成功：{dialog.FileName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnRestoreClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "数据库文件|*.db",
                Title = "恢复数据"
            };

            if (dialog.ShowDialog() == true)
            {
                if (MessageBox.Show("恢复数据将覆盖当前所有数据，确定继续？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    if (_dataService.RestoreBackup(dialog.FileName))
                    {
                        LoadData();
                        UpdateTodayView();
                        MessageBox.Show("恢复成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("恢复失败，请检查备份文件是否有效", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        #endregion

        #region TimeSlot
        private void LoadTimeSlotData()
        {
            TimeSlotDataGrid.ItemsSource = _dataService.GetAllTimeSlotProjects();
        }

        private void OnAddTimeSlotClick(object sender, RoutedEventArgs e)
        {
            var dialog = new TimeSlotDialog(_dataService);
            if (dialog.ShowDialog() == true)
            {
                _dataService.AddTimeSlotProject(dialog.TimeSlot);
                LoadTimeSlotData();
            }
        }

        private void OnEditTimeSlotClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TimeSlotProject timeSlot)
            {
                var dialog = new TimeSlotDialog(_dataService, timeSlot);
                if (dialog.ShowDialog() == true)
                {
                    _dataService.UpdateTimeSlotProject(dialog.TimeSlot);
                    LoadTimeSlotData();
                }
            }
        }

        private void OnDeleteTimeSlotClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TimeSlotProject timeSlot)
            {
                if (MessageBox.Show($"确定删除时间段 \"{timeSlot.Name}\"？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _dataService.DeleteTimeSlotProject(timeSlot.Id);
                    LoadTimeSlotData();
                }
            }
        }
        #endregion

        #region Tray
        private void OnTrayDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void OnShowWindowClick(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void OnShowTopBarClick(object sender, RoutedEventArgs e)
        {
            ShowTopBar();
        }

        private void OnHideTopBarClick(object sender, RoutedEventArgs e)
        {
            HideTopBar();
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion

        #region TopBar
        public void ShowTopBar()
        {
            if (_topBarWindow == null)
            {
                _topBarWindow = new TopBarWindow(_dataService, this);
                _topBarWindow.SetOpacity(_settings.TopBarOpacity);
                _topBarWindow.SetHeight(_settings.TopBarHeight);
                _topBarWindow.SetPosition(_settings.TopBarPosition);
            }
            _topBarWindow.Show();
        }

        public void HideTopBar()
        {
            _topBarWindow?.Hide();
        }

        public void CloseTopBar()
        {
            _topBarWindow?.Close();
            _topBarWindow = null;
        }
        #endregion
    }

    public class TodayDutyItem
    {
        public string ProjectName { get; set; } = "";
        public string StudentsText { get; set; } = "";
        public string PersonCountText { get; set; } = "";
    }
}
