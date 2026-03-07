using ClassDutyHelper.Models;
using ClassDutyHelper.Services;
using System.Windows;

namespace ClassDutyHelper.Views
{
    public partial class EditCarouselDialog : Window
    {
        private readonly DataService _dataService;
        private readonly CarouselItem _item;

        public EditCarouselDialog(DataService dataService, CarouselItem item)
        {
            InitializeComponent();
            _dataService = dataService;
            _item = item;
            LoadData();
        }

        private void LoadData()
        {
            ContentTextBox.Text = _item.Content;
            SortOrderTextBox.Text = _item.SortOrder.ToString();
            IsEnabledCheckBox.IsChecked = _item.IsEnabled;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
            {
                MessageBox.Show("请输入轮播内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _item.Content = ContentTextBox.Text.Trim();
            _item.SortOrder = int.TryParse(SortOrderTextBox.Text, out var sortOrder) ? sortOrder : 0;
            _item.IsEnabled = IsEnabledCheckBox.IsChecked == true;
            _item.UpdatedAt = DateTime.Now;

            _dataService.UpdateCarouselItem(_item);
            DialogResult = true;
            Close();
        }
    }
}
