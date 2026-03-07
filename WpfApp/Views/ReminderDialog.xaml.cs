using ClassDutyHelper.Models;
using System.Windows;

namespace ClassDutyHelper.Views
{
    public partial class ReminderDialog : Window
    {
        public Reminder Reminder { get; private set; }

        public ReminderDialog(Reminder? reminder = null)
        {
            InitializeComponent();
            Reminder = reminder ?? new Reminder();

            NameTextBox.Text = Reminder.Name;
            TimeTextBox.Text = Reminder.Time.ToString(@"hh\:mm");
            AdvanceTextBox.Text = Reminder.AdvanceMinutes.ToString();
            IsEnabledCheckBox.IsChecked = Reminder.IsEnabled;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("请输入提醒名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(TimeTextBox.Text, out var time))
            {
                MessageBox.Show("时间格式错误，请使用 HH:mm 格式", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(AdvanceTextBox.Text, out var advance) || advance < 0)
            {
                MessageBox.Show("提前预警分钟数必须大于等于0", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Reminder.Name = NameTextBox.Text.Trim();
            Reminder.Time = time;
            Reminder.AdvanceMinutes = advance;
            Reminder.IsEnabled = IsEnabledCheckBox.IsChecked == true;

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
