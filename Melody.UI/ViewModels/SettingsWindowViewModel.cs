// Copyright (c) Matula Márton. All rights reserved.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Melody.UI.ViewModels
{
    public class SettingsWindowViewModel : ObservableRecipient
    {
        public SettingsWindowViewModel()
        {
            this.GoHomeCommand = new RelayCommand(() => this.NavigationRequested?.Invoke(this, "Menu"));
        }

        /// <summary>Occurs when navigation is requested.</summary>
        public event EventHandler<string> NavigationRequested;

        public ObservableCollection<string> AvailableThemes { get; } = new ObservableCollection<string> { "Default", "Dark", "Light" };

        private string selectedTheme = "Default";

        public string SelectedTheme
        {
            get => selectedTheme;
            set => SetProperty(ref selectedTheme, value);
        }

        private bool isFullPianoWidth = false;

        public bool IsFullPianoWidth
        {
            get => isFullPianoWidth;
            set
            {
                if (SetProperty(ref isFullPianoWidth, value))
                {
                    IsDynamicPianoWidth = !value;
                }
            }
        }

        private bool isDynamicPianoWidth = true;

        public bool IsDynamicPianoWidth
        {
            get => isDynamicPianoWidth;
            set
            {
                if (SetProperty(ref isDynamicPianoWidth, value))
                {
                    IsFullPianoWidth = !value;
                }
            }
        }

        // Parancsok a gombokhoz
        public ICommand SaveSettingsCommand { get; }

        public ICommand GoHomeCommand { get; }
    }
}
