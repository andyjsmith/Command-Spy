using System.Windows;
using System.Windows.Input;

namespace CommandSpy
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var filter in ((MainWindow)Application.Current.MainWindow).filters)
            {
                listBox.Items.Add(filter);
            }
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                AddItem();
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddItem();
        }

        private void AddItem()
        {
            if (textBox.Text.Length == 0) { return; }
            listBox.Items.Add(textBox.Text.ToLower());
            textBox.Text = "";
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            listBox.Items.Remove(listBox.SelectedItem);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            listBox.Items.Clear();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ChangeFilter(listBox.Items);
            Close();
        }
    }
}
