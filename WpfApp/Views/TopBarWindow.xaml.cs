using ClassDutyHelper.Services;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace ClassDutyHelper.Views
{
    public partial class TopBarWindow : Window
    {
        // Windows API for click-through
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private readonly DataService _dataService;
        private readonly MainWindow _mainWindow;
        private readonly DispatcherTimer _scrollTimer;
        private readonly DispatcherTimer _verticalScrollTimer;
        private readonly DispatcherTimer _refreshTimer;
        private readonly DispatcherTimer _timeTimer;
        private readonly DispatcherTimer _reminderTimer;
        private string _fullDutyText = "";
        private double _currentOpacity = 0.9;
        private int _currentHeight = 50;
        private int _currentWidth = 38;
        private double _scrollOffset = 0;
        private double _verticalScrollOffset = 0;
        private int _position = 0;
        private string _currentReminderText = "";
        private bool _isShowingReminder = false;
        private DateTime? _reminderStartTime;
        private bool _isShowingWarning = false;

        public TopBarWindow(DataService dataService, MainWindow mainWindow)
        {
            InitializeComponent();
            _dataService = dataService;
            _mainWindow = mainWindow;

            _scrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _scrollTimer.Tick += OnScrollTick;

            _verticalScrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _verticalScrollTimer.Tick += OnVerticalScrollTick;

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _refreshTimer.Tick += OnRefreshTick;
            _refreshTimer.Start();

            _timeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timeTimer.Tick += OnTimeTick;
            _timeTimer.Start();

            _reminderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _reminderTimer.Tick += OnReminderTick;
            _reminderTimer.Start();

            var settings = _dataService.GetAppSettings();
            _currentOpacity = settings.TopBarOpacity;
            _currentHeight = settings.TopBarHeight;
            _position = settings.TopBarPosition;
            MainBorder.Opacity = _currentOpacity;

            UpdatePosition();
            UpdateTime();
            UpdateInfo();
        }

        public void SetPosition(int position)
        {
            _position = position;
            UpdatePosition();
            UpdateTime();
            UpdateInfo();
        }

        private void UpdatePosition()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var workArea = SystemParameters.WorkArea;

            HorizontalPanel.Visibility = Visibility.Collapsed;
            VerticalPanel.Visibility = Visibility.Collapsed;
            _scrollTimer.Stop();
            _verticalScrollTimer.Stop();

            switch (_position)
            {
                case 0:
                    HorizontalPanel.Visibility = Visibility.Visible;
                    VerticalPanel.Visibility = Visibility.Collapsed;
                    TimeModule.Visibility = Visibility.Visible;
                    InnerGrid.Margin = new Thickness(10, 0, 10, 0);
                    InnerGrid.VerticalAlignment = VerticalAlignment.Center;
                    Width = screenWidth;
                    Height = _currentHeight;
                    Left = 0;
                    Top = 0;
                    MainBorder.CornerRadius = new CornerRadius(0, 0, 8, 8);
                    ShadowEffect.Direction = 270;
                    break;
                case 1:
                    HorizontalPanel.Visibility = Visibility.Visible;
                    VerticalPanel.Visibility = Visibility.Collapsed;
                    TimeModule.Visibility = Visibility.Visible;
                    InnerGrid.Margin = new Thickness(10, 0, 10, 0);
                    InnerGrid.VerticalAlignment = VerticalAlignment.Center;
                    Width = screenWidth;
                    Height = _currentHeight;
                    Left = 0;
                    Top = workArea.Bottom - _currentHeight;
                    MainBorder.CornerRadius = new CornerRadius(8, 8, 0, 0);
                    ShadowEffect.Direction = 90;
                    break;
                case 2:
                    HorizontalPanel.Visibility = Visibility.Collapsed;
                    VerticalPanel.Visibility = Visibility.Visible;
                    TimeModuleV.Visibility = Visibility.Collapsed;
                    TimeItemsV.Visibility = Visibility.Collapsed;
                    DateItemsV.Visibility = Visibility.Collapsed;
                    InnerGrid.Margin = new Thickness(4, 6, 4, 6);
                    InnerGrid.VerticalAlignment = VerticalAlignment.Top;
                    Width = _currentWidth;
                    Height = screenHeight;
                    Left = 0;
                    Top = 0;
                    MainBorder.CornerRadius = new CornerRadius(0, 8, 8, 0);
                    ShadowEffect.Direction = 0;
                    break;
                case 3:
                    HorizontalPanel.Visibility = Visibility.Collapsed;
                    VerticalPanel.Visibility = Visibility.Visible;
                    TimeModuleV.Visibility = Visibility.Collapsed;
                    TimeItemsV.Visibility = Visibility.Collapsed;
                    DateItemsV.Visibility = Visibility.Collapsed;
                    InnerGrid.Margin = new Thickness(4, 6, 4, 6);
                    InnerGrid.VerticalAlignment = VerticalAlignment.Top;
                    Width = _currentWidth;
                    Height = screenHeight;
                    Left = workArea.Right - _currentWidth;
                    Top = 0;
                    MainBorder.CornerRadius = new CornerRadius(8, 0, 0, 8);
                    ShadowEffect.Direction = 180;
                    break;
            }

            InvalidateVisual();
            UpdateLayout();
        }

        private void OnTimeTick(object? sender, EventArgs e)
        {
            UpdateTime();
            UpdateInfo();
        }

        private void OnReminderTick(object? sender, EventArgs e)
        {
            CheckReminders();
        }

        private void CheckReminders()
        {
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;
            var reminders = _dataService.GetEnabledReminders();

            // 调试信息
            System.Diagnostics.Debug.WriteLine($"CheckReminders: CurrentTime={currentTime}, RemindersCount={reminders.Count}");
            foreach (var r in reminders)
            {
                System.Diagnostics.Debug.WriteLine($"Reminder: {r.Name}, Time={r.Time}, Enabled={r.IsEnabled}, Advance={r.AdvanceMinutes}");
            }

            var activeReminder = reminders.FirstOrDefault(r => 
                r.Time.Hours == currentTime.Hours && 
                r.Time.Minutes == currentTime.Minutes &&
                currentTime.Seconds < 2);

            if (activeReminder != null && !_isShowingReminder)
            {
                System.Diagnostics.Debug.WriteLine($"Active reminder found: {activeReminder.Name}");
                _currentReminderText = activeReminder.Name;
                _isShowingReminder = true;
                _reminderStartTime = now;
                _isShowingWarning = false;
                ShowReminder();
            }

            var warningReminder = reminders.FirstOrDefault(r =>
            {
                var warningTime = r.Time.Subtract(TimeSpan.FromMinutes(r.AdvanceMinutes));
                System.Diagnostics.Debug.WriteLine($"Warning check: {r.Name}, WarningTime={warningTime}, CurrentTime={currentTime}");
                return warningTime.Hours == currentTime.Hours &&
                       warningTime.Minutes == currentTime.Minutes &&
                       currentTime.Seconds < 2;
            });

            if (warningReminder != null && !_isShowingWarning && !_isShowingReminder)
            {
                System.Diagnostics.Debug.WriteLine($"Warning reminder found: {warningReminder.Name}");
                _currentReminderText = $"⚠️ {warningReminder.Name} ({warningReminder.AdvanceMinutes}分钟后)";
                _isShowingWarning = true;
                _reminderStartTime = now;
                ShowReminder();
            }

            if (_reminderStartTime.HasValue && (now - _reminderStartTime.Value).TotalSeconds >= 10)
            {
                System.Diagnostics.Debug.WriteLine("Hiding reminder");
                _isShowingReminder = false;
                _isShowingWarning = false;
                _reminderStartTime = null;
                HideReminder();
            }
        }

        private void ShowReminder()
        {
            if (_position == 2 || _position == 3)
            {
                // 左右悬浮栏
                DutyItemsV.Items.Clear();
                
                if (_isShowingWarning)
                {
                    // 提前预警：只显示闹钟图标
                    var textBlock = CreateVerticalTextBlock("⏰", 16, true, Colors.White);
                    
                    // 闪烁效果
                    var flashAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.3,
                        Duration = TimeSpan.FromMilliseconds(500),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    
                    textBlock.BeginAnimation(TextBlock.OpacityProperty, flashAnimation);
                    DutyItemsV.Items.Add(textBlock);
                }
                else if (_isShowingReminder)
                {
                    // 正式提醒：文字变黄
                    foreach (var c in _currentReminderText)
                    {
                        var textBlock = CreateVerticalTextBlock(c.ToString(), 11, false, Colors.Yellow);
                        DutyItemsV.Items.Add(textBlock);
                    }
                }
            }
            else
            {
                // 上下悬浮栏
                DutyInfoText.Text = _currentReminderText;
                DutyInfoText.Margin = new Thickness(15, 0, 0, 0);
                
                if (_isShowingWarning)
                {
                    // 提前预警：使用简单的闪烁效果
                    DutyInfoText.Foreground = new SolidColorBrush(Colors.Yellow);
                    
                    var flashAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.3,
                        Duration = TimeSpan.FromMilliseconds(500),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    
                    DutyInfoText.BeginAnimation(TextBlock.OpacityProperty, flashAnimation);
                }
                else if (_isShowingReminder)
                {
                    // 正式提醒：使用酷炫动画效果
                    
                    // 创建颜色动画 - 在黄色和橙色之间渐变
                    var colorAnimation = new ColorAnimation
                    {
                        From = Colors.Yellow,
                        To = Color.FromRgb(255, 165, 0), // 橙色
                        Duration = TimeSpan.FromMilliseconds(600),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    
                    var brush = new SolidColorBrush(Colors.Yellow);
                    DutyInfoText.Foreground = brush;
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                    
                    // 添加缩放动画效果
                    var scaleTransform = new ScaleTransform(1.0, 1.0);
                    DutyInfoText.RenderTransform = scaleTransform;
                    DutyInfoText.RenderTransformOrigin = new Point(0.5, 0.5);
                    
                    var scaleAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.15,
                        Duration = TimeSpan.FromMilliseconds(400),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                        EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 5 }
                    };
                    
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                    
                    // 添加发光效果
                    var glowAnimation = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(500),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    
                    var glowEffect = new DropShadowEffect
                    {
                        Color = Colors.Yellow,
                        BlurRadius = 20,
                        ShadowDepth = 0,
                        Opacity = 0.0
                    };
                    DutyInfoText.Effect = glowEffect;
                    glowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, glowAnimation);
                }
            }
        }

        private void HideReminder()
        {
            _isShowingReminder = false;
            _isShowingWarning = false;
            _reminderStartTime = null;

            if (_position == 2 || _position == 3)
            {
                // 左右悬浮栏：清除动画并恢复显示
                UpdateInfo();
            }
            else
            {
                // 上下悬浮栏：清除所有动画效果
                DutyInfoText.BeginAnimation(TextBlock.OpacityProperty, null);
                DutyInfoText.RenderTransform = null;
                DutyInfoText.Effect = null;
                
                // 恢复默认样式
                DutyInfoText.Foreground = new SolidColorBrush(Colors.White);
                DutyInfoText.Opacity = 1.0;
                DutyInfoText.Margin = new Thickness(0);
                
                UpdateInfo();
            }
        }

        private void UpdateTime()
        {
            var now = DateTime.Now;
            var timeStr = now.ToString("HH:mm:ss");
            var dateStr = now.ToString("MM月dd日");
            var weekStr = GetWeekDayText(now);

            TimeText.Text = timeStr;
            DateText.Text = dateStr;
            WeekText.Text = weekStr;

            TimeItemsV.Items.Clear();
            foreach (var c in timeStr)
            {
                TimeItemsV.Items.Add(CreateVerticalTextBlock(c.ToString(), 13, true, Colors.White));
            }

            DateItemsV.Items.Clear();
            foreach (var c in dateStr + " " + weekStr)
            {
                DateItemsV.Items.Add(CreateVerticalTextBlock(c.ToString(), 10, false, Color.FromRgb(204, 204, 204)));
            }
        }

        private TextBlock CreateVerticalTextBlock(string text, int fontSize, bool bold, Color color)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush(color),
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private TextBlock CreateNormalTextBlock(string text, int fontSize, bool bold, Color color)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush(color),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 4)
            };
        }

        public void UpdateInfo()
        {
            // 如果正在显示提醒，则跳过更新
            if (_isShowingReminder || _isShowingWarning)
            {
                return;
            }
            
            var today = DateTime.Today;
            var records = _dataService.GetDutyRecordsByDate(today);

            var projectIds = _dataService.GetProjectIdsForCurrentTimeSlot();
            var filterByTimeSlot = projectIds.Count > 0;

            if (records.Count == 0)
            {
                var noDutyText = "今日暂无值日安排";
                DutyInfoText.Text = noDutyText;
                DutyItemsV.Items.Clear();
                foreach (var c in noDutyText)
                {
                    DutyItemsV.Items.Add(CreateVerticalTextBlock(c.ToString(), 11, false, Colors.White));
                }
                _fullDutyText = "";
                _scrollTimer.Stop();
                _verticalScrollTimer.Stop();
            }
            else
            {
                var grouped = records.GroupBy(r => r.DutyProjectId);
                
                if (filterByTimeSlot)
                {
                    grouped = grouped.Where(g => projectIds.Contains(g.Key));
                }

                var parts = grouped.Select(g =>
                {
                    var project = g.First().DutyProject?.Name ?? "";
                    var students = string.Join("、", g.Select(r => r.Student?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)));
                    return $"{project}：{students}";
                });

                var partsList = parts.ToList();
                if (partsList.Count == 0)
                {
                    var noDutyText = filterByTimeSlot ? "当前时段暂无值日" : "今日暂无值日安排";
                    DutyInfoText.Text = noDutyText;
                    DutyItemsV.Items.Clear();
                    foreach (var c in noDutyText)
                    {
                        DutyItemsV.Items.Add(CreateVerticalTextBlock(c.ToString(), 11, false, Colors.White));
                    }
                    _fullDutyText = "";
                    _scrollTimer.Stop();
                    _verticalScrollTimer.Stop();
                    return;
                }

                _fullDutyText = string.Join("    ", partsList);
                DutyInfoText.Text = _fullDutyText;

                DutyItemsV.Items.Clear();
                var dutyText = string.Join(" ", partsList);
                foreach (var c in dutyText)
                {
                    DutyItemsV.Items.Add(CreateVerticalTextBlock(c.ToString(), 11, false, Colors.White));
                }

                if (_position == 0 || _position == 1)
                {
                    DutyScrollViewer.ScrollToHorizontalOffset(0);
                    _scrollOffset = 0;
                    _scrollTimer.Start();
                }
                else
                {
                    MainScrollViewer.ScrollToVerticalOffset(0);
                    _verticalScrollOffset = 0;
                    _verticalScrollTimer.Start();
                }
            }
        }

        private string GetWeekDayText(DateTime date)
        {
            var weekdays = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            return weekdays[(int)date.DayOfWeek];
        }

        private void OnScrollTick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_fullDutyText))
            {
                _scrollTimer.Stop();
                return;
            }

            var textWidth = DutyInfoText.ActualWidth;
            var viewerWidth = DutyScrollViewer.ActualWidth;

            if (textWidth <= viewerWidth)
            {
                _scrollTimer.Stop();
                return;
            }

            _scrollOffset += 1;

            if (_scrollOffset > textWidth - viewerWidth + 100)
            {
                _scrollOffset = 0;
            }

            DutyScrollViewer.ScrollToHorizontalOffset(_scrollOffset);
        }

        private void OnVerticalScrollTick(object? sender, EventArgs e)
        {
            TimeItemsV.UpdateLayout();
            DateItemsV.UpdateLayout();
            DutyItemsV.UpdateLayout();

            var timeHeight = TimeItemsV.ActualHeight;
            var dateHeight = DateItemsV.ActualHeight;
            var dutyHeight = DutyItemsV.ActualHeight;
            var totalHeight = timeHeight + dateHeight + dutyHeight;
            var viewerHeight = MainScrollViewer.ActualHeight;

            if (totalHeight <= viewerHeight)
            {
                _verticalScrollTimer.Stop();
                return;
            }

            _verticalScrollOffset += 0.5;

            if (_verticalScrollOffset > dutyHeight + timeHeight + dateHeight - viewerHeight + 100)
            {
                _verticalScrollOffset = 0;
            }

            MainScrollViewer.ScrollToVerticalOffset(_verticalScrollOffset);
        }

        private void OnDutyScroll(object sender, MouseWheelEventArgs e)
        {
            _scrollOffset -= e.Delta / 3;

            if (_scrollOffset < 0) _scrollOffset = 0;

            var textWidth = DutyInfoText.ActualWidth;
            var viewerWidth = DutyScrollViewer.ActualWidth;
            var maxOffset = textWidth - viewerWidth + 50;

            if (_scrollOffset > maxOffset) _scrollOffset = maxOffset;

            DutyScrollViewer.ScrollToHorizontalOffset(_scrollOffset);
        }

        private void OnRefreshTick(object? sender, EventArgs e)
        {
            UpdateInfo();
        }

        public void SetOpacity(double opacity)
        {
            _currentOpacity = opacity;
            MainBorder.Opacity = opacity;
        }

        public void SetHeight(int height)
        {
            _currentHeight = height;
            if (_position == 0 || _position == 1)
            {
                Height = _currentHeight;
                UpdatePosition();
            }
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            MainBorder.Opacity = Math.Max(0.3, _currentOpacity * 0.5);
            
            // 启用点击穿透
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            MainBorder.Opacity = _currentOpacity;
            
            // 禁用点击穿透
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow == null) return;
            
            if (_position == 2 || _position == 3)
            {
                return;
            }
            
            _mainWindow.Visibility = Visibility.Visible;
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.Focus();
        }

        private void OnDutyModuleClick(object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow == null) return;
            
            _mainWindow.Visibility = Visibility.Visible;
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.Focus();
            e.Handled = true;
        }

        private void OnTimeModuleClick(object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow == null) return;
            
            _mainWindow.Visibility = Visibility.Visible;
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.Focus();
            e.Handled = true;
        }
    }
}
