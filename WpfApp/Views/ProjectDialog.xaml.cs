using ClassDutyHelper.Models;
using System.Windows;

namespace ClassDutyHelper.Views
{
    public partial class ProjectDialog : Window
    {
        public DutyProject Project { get; private set; }

        public ProjectDialog(DutyProject? project = null)
        {
            InitializeComponent();
            Project = project ?? new DutyProject();

            NameTextBox.Text = Project.Name;
            CountTextBox.Text = Project.DefaultPersonCount.ToString();
            IsEnabledCheckBox.IsChecked = Project.IsEnabled;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("请输入项目名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(CountTextBox.Text, out var count) || count < 1)
            {
                MessageBox.Show("人数必须大于0", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Project.Name = NameTextBox.Text.Trim();
            Project.DefaultPersonCount = count;
            Project.IsEnabled = IsEnabledCheckBox.IsChecked == true;

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
