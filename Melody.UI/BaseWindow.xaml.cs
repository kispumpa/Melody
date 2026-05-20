// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI
{
    using System.Windows;
    using Melody.UI.ViewModels;

    /// <summary>Base window class.</summary>
    public partial class BaseWindow : Window
    {
        /// <summary>Initializes a new instance of the <see cref="BaseWindow"/> class.</summary>
        public BaseWindow()
        {
            this.InitializeComponent();
            this.DataContext = new BaseWindowViewModel();
        }
    }
}
