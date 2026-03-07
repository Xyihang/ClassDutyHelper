using ClassDutyHelper.Models;
using ClassDutyHelper.Services;
using System.Windows;

namespace ClassDutyHelper.Views
{
    public partial class CarouselDialog : Window
    {
        private readonly DataService _dataService;
        private List<CarouselItem> _carouselItems = new List<CarouselItem>();

        public CarouselDialog(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            _carouselItems = _dataService.GetAllCarouselItems();
            CarouselDataGrid.ItemsSource = _carouselItems;
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
            {
                MessageBox.Show("请输入轮播内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var item = new CarouselItem
            {
                Content = ContentTextBox.Text.Trim(),
                SortOrder = int.TryParse(SortOrderTextBox.Text, out var sortOrder) ? sortOrder : 0,
                IsEnabled = IsEnabledCheckBox.IsChecked == true
            };

            _dataService.AddCarouselItem(item);
            LoadData();
            ClearInput();
        }

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (CarouselDataGrid.SelectedItem is not CarouselItem selectedItem) return;

            var dialog = new EditCarouselDialog(_dataService, selectedItem);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (CarouselDataGrid.SelectedItem is not CarouselItem selectedItem) return;

            var result = MessageBox.Show($"确定要删除这条轮播内容吗？\n\n{selectedItem.Content}", 
                "确认删除", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _dataService.DeleteCarouselItem(selectedItem.Id);
                LoadData();
            }
        }

        private void OnMoveUpClick(object sender, RoutedEventArgs e)
        {
            if (CarouselDataGrid.SelectedItem is not CarouselItem selectedItem) return;

            var currentIndex = _carouselItems.IndexOf(selectedItem);
            if (currentIndex > 0)
            {
                var previousItem = _carouselItems[currentIndex - 1];
                var tempOrder = selectedItem.SortOrder;
                selectedItem.SortOrder = previousItem.SortOrder;
                previousItem.SortOrder = tempOrder;

                _dataService.UpdateCarouselItem(selectedItem);
                _dataService.UpdateCarouselItem(previousItem);
                LoadData();
            }
        }

        private void OnMoveDownClick(object sender, RoutedEventArgs e)
        {
            if (CarouselDataGrid.SelectedItem is not CarouselItem selectedItem) return;

            var currentIndex = _carouselItems.IndexOf(selectedItem);
            if (currentIndex < _carouselItems.Count - 1)
            {
                var nextItem = _carouselItems[currentIndex + 1];
                var tempOrder = selectedItem.SortOrder;
                selectedItem.SortOrder = nextItem.SortOrder;
                nextItem.SortOrder = tempOrder;

                _dataService.UpdateCarouselItem(selectedItem);
                _dataService.UpdateCarouselItem(nextItem);
                LoadData();
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearInput()
        {
            ContentTextBox.Text = "";
            SortOrderTextBox.Text = "0";
            IsEnabledCheckBox.IsChecked = true;
        }
    }
}
