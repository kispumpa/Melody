// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI.ViewModels
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>Represents the base view model for a window.</summary>
    public class BaseWindowViewModel : INotifyPropertyChanged
    {
        private object currentView;

        /// <summary>Initializes a new instance of the <see cref="BaseWindowViewModel"/> class.</summary>
        public BaseWindowViewModel()
        {
            this.MenuWindowVM = new MenuWindowViewModel();
            this.MenuWindowVM.NavigationRequested += this.OnMenuNavigationRequested;

            this.CurrentView = this.MenuWindowVM;
        }

        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Gets or sets the current view.</summary>
        public object CurrentView
        {
            get => this.currentView;
            set
            {
                this.currentView = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.WindowTitle));
            }
        }

        /// <summary>Gets the title of the current window.</summary>
        public string WindowTitle
        {
            get => this.CurrentView switch
            {
                MenuWindowViewModel => "Melody - Menu",
                PlayerWindowViewModel => "Melody - Player",
                PracticeWindowViewModel => "Melody - Practice",
                SettingsWindowViewModel => "Melody - Settings",
                _ => "Melody"
            };
        }

        /// <summary>Gets or sets the menu window view model.</summary>
        public MenuWindowViewModel MenuWindowVM { get; set; }

        /// <summary>Gets or sets the main window view model.</summary>
        public PlayerWindowViewModel PlayerWindowVM { get; set; }

        /// <summary>Gets or sets the practice window view model.</summary>
        public PracticeWindowViewModel PracticeWindowVM { get; set; }

        /// <summary>Gets or sets the settings window view model.</summary>
        public SettingsWindowViewModel SettingsWindowVM { get; set; }

        // Ide majd bekerülhet a SettingsViewModel is
        // public SettingsViewModel SettingsVM { get; set; }

        /// <summary>Raises the <see cref="PropertyChanged"/> event.</summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnMenuNavigationRequested(object sender, string viewName)
        {
            switch (viewName)
            {
                case "Practice":
                    this.PracticeWindowVM = new PracticeWindowViewModel();
                    this.PracticeWindowVM.NavigationRequested += this.OnChildNavigationRequested;

                    this.CurrentView = this.PracticeWindowVM;
                    break;
                case "Player":
                    this.PlayerWindowVM = new PlayerWindowViewModel();
                    this.PlayerWindowVM.NavigationRequested += this.OnChildNavigationRequested;

                    this.CurrentView = this.PlayerWindowVM;
                    break;

                case "Settings":
                    this.SettingsWindowVM = new SettingsWindowViewModel();
                    this.SettingsWindowVM.NavigationRequested += this.OnChildNavigationRequested;

                    this.CurrentView = this.SettingsWindowVM;
                    break;
            }
        }

        private void OnChildNavigationRequested(object sender, string viewName)
        {
            if (viewName == "Menu")
            {
                if (this.PlayerWindowVM != null)
                {
                    this.PlayerWindowVM.NavigationRequested -= this.OnChildNavigationRequested;
                    this.PlayerWindowVM = null;
                }

                if (this.PracticeWindowVM != null)
                {
                    this.PracticeWindowVM.NavigationRequested -= this.OnChildNavigationRequested;
                    this.PracticeWindowVM = null;
                }

                if (this.SettingsWindowVM != null)
                {
                    this.SettingsWindowVM.NavigationRequested -= this.OnChildNavigationRequested;
                    this.SettingsWindowVM = null;
                }

                this.CurrentView = this.MenuWindowVM;
            }
        }
    }
}
