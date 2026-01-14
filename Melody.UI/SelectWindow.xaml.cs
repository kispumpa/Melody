using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Melody.UI
{
    /// <summary>
    /// Interaction logic for SelectWindow.xaml
    /// </summary>
    public partial class SelectWindow : Window
    {
        public string SelectedKey { get; private set; }

        public SelectWindow(Dictionary<string, string> sheets)
        {
            InitializeComponent();

            ItemListBox.ItemsSource = sheets;
        }

        private void ItemListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemListBox.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                this.SelectedKey = selectedItem.Key;

                this.DialogResult = true;
            }
        }
    }
}
