using ClassDutyHelper.Models;
using System.Windows;

namespace ClassDutyHelper.Views
{
    public partial class StudentDialog : Window
    {
        public Student Student { get; private set; }

        public StudentDialog(Student? student = null)
        {
            InitializeComponent();
            Student = student ?? new Student();

            NameTextBox.Text = Student.Name;
            StudentIdTextBox.Text = Student.StudentId ?? "";
            GroupTextBox.Text = Student.Group ?? "";
            IsEnabledCheckBox.IsChecked = Student.IsEnabled;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("请输入姓名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Student.Name = NameTextBox.Text.Trim();
            Student.StudentId = StudentIdTextBox.Text.Trim();
            Student.Group = GroupTextBox.Text.Trim();
            Student.IsEnabled = IsEnabledCheckBox.IsChecked == true;

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
