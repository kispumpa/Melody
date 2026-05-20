// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>A window for selecting an item from a list of sheets.</summary>
    public partial class SelectWindow : Window
    {
        /// <summary>Initializes a new instance of the <see cref="SelectWindow"/> class.</summary>
        /// <param name="sheets">The dictionary of sheets to display.</param>
        public SelectWindow(Dictionary<string, string> sheets)
        {
            this.InitializeComponent();

            this.ItemListBox.ItemsSource = sheets;
        }

        /// <summary>Gets the key of the selected item.</summary>
        public string SelectedKey { get; private set; }

        private void ItemListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ItemListBox.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                this.SelectedKey = selectedItem.Key;

                this.DialogResult = true;
            }
        }
    }
}
