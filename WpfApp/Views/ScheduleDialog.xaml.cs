using ClassDutyHelper.Models;
using System.Windows;

namespace ClassDutyHelper.Views
{
    public partial class ScheduleDialog : Window
    {
        public DutyRecord Record { get; private set; }
        private readonly List<Student> _students;
        private readonly List<DutyProject> _projects;

        public ScheduleDialog(DutyRecord record, List<Student> students, List<DutyProject> projects)
        {
            InitializeComponent();
            Record = record;
            _students = students;
            _projects = projects;

            ProjectComboBox.ItemsSource = _projects;
            StudentComboBox.ItemsSource = _students;

            ProjectComboBox.SelectedValue = Record.DutyProjectId;
            StudentComboBox.SelectedValue = Record.StudentId;
            RemarkTextBox.Text = Record.Remark ?? "";
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (ProjectComboBox.SelectedValue == null)
            {
                MessageBox.Show("请选择值日项目", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StudentComboBox.SelectedValue == null)
            {
                MessageBox.Show("请选择值日人员", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Record.DutyProjectId = (int)ProjectComboBox.SelectedValue;
            Record.StudentId = (int)StudentComboBox.SelectedValue;
            Record.Remark = RemarkTextBox.Text.Trim();

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
