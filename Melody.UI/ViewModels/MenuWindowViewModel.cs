// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI.ViewModels
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.Input;

    /// <summary>ViewModel for the menu window.</summary>
    public class MenuWindowViewModel : INotifyPropertyChanged
    {
        /// <summary>Initializes a new instance of the <see cref="MenuWindowViewModel"/> class.</summary>
        public MenuWindowViewModel()
        {
            this.StartPracticeCommand = new RelayCommand(this.OnStartPractice);
            this.PlayerCommand = new RelayCommand(this.OnPlayer);
            this.SettingsCommand = new RelayCommand(this.OnSettings);
            this.ExitCommand = new RelayCommand(this.OnExit);
        }

        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Occurs when navigation is requested.</summary>
        public event EventHandler<string> NavigationRequested;

        /// <summary>Gets the command to start practice.</summary>
        public ICommand StartPracticeCommand { get; }

        /// <summary>Gets or sets the command to open the player.</summary>
        public ICommand PlayerCommand { get; set; }

        /// <summary>Gets the command to open the settings.</summary>
        public ICommand SettingsCommand { get; }

        /// <summary>Gets the command to exit the application.</summary>
        public ICommand ExitCommand { get; }

        /// <summary>Raises the <see cref="PropertyChanged"/> event.</summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnStartPractice()
        {
            this.NavigationRequested?.Invoke(this, "Practice");
        }

        private void OnPlayer()
        {
            this.NavigationRequested?.Invoke(this, "Player");
        }

        private void OnSettings()
        {
            this.NavigationRequested?.Invoke(this, "Settings");
        }

        private void OnExit()
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
