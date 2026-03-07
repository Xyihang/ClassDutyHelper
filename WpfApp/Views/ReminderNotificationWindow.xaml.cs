using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ClassDutyHelper.Views
{
    public partial class ReminderNotificationWindow : Window
    {
        private readonly DispatcherTimer _autoCloseTimer;
        private Storyboard? _flashStoryboard;
        private Storyboard? _shakeStoryboard;

        public ReminderNotificationWindow(string title, string message)
        {
            InitializeComponent();
            
            TitleText.Text = title;
            MessageText.Text = message;

            _flashStoryboard = (Storyboard)FindResource("FlashAnimation");
            _shakeStoryboard = (Storyboard)FindResource("ShakeAnimation");

            _flashStoryboard.Begin();
            _shakeStoryboard.Begin();

            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _autoCloseTimer.Tick += (s, e) =>
            {
                Close();
            };
            _autoCloseTimer.Start();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            _flashStoryboard?.Stop();
            _shakeStoryboard?.Stop();
            _autoCloseTimer?.Stop();
            Close();
        }
    }
}
