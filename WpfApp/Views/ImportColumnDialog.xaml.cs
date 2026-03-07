using System.Windows;
using System.Windows.Controls;

namespace ClassDutyHelper.Views
{
    public partial class ImportColumnDialog : Window
    {
        public int[] Columns { get; private set; }
        private readonly string[] _labels;
        private readonly TextBox[] _textBoxes;

        public ImportColumnDialog(string title, string[] labels)
        {
            InitializeComponent();
            TitleText.Text = title;
            _labels = labels;
            _textBoxes = new TextBox[labels.Length];
            Columns = new int[labels.Length];

            for (int i = 0; i < labels.Length; i++)
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var label = new TextBlock { Text = labels[i], VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                var textBox = new TextBox
                {
                    Style = (Style)FindResource("ModernTextBoxStyle"),
                    Text = (i + 1).ToString()
                };
                Grid.SetColumn(textBox, 1);
                grid.Children.Add(textBox);
                _textBoxes[i] = textBox;

                ColumnsPanel.Children.Add(grid);
            }
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _textBoxes.Length; i++)
            {
                if (int.TryParse(_textBoxes[i].Text, out var col))
                {
                    Columns[i] = col;
                }
                else
                {
                    Columns[i] = 0;
                }
            }

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
