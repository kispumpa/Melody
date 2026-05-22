// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI.ViewModels
{
    using System.Collections.ObjectModel;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    /// <summary>ViewModel for the settings window.</summary>
    public class SettingsWindowViewModel : ObservableRecipient
    {
        private bool isFullPianoWidth = false;
        private bool isDynamicPianoWidth = true;
        private string selectedTheme = "Default";
        private string selectedLanguage = "English";

        /// <summary>Initializes a new instance of the <see cref="SettingsWindowViewModel"/> class.</summary>
        public SettingsWindowViewModel()
        {
            this.SaveSettingsCommand = new RelayCommand(this.SaveSettings);
            this.GoHomeCommand = new RelayCommand(() => this.NavigationRequested?.Invoke(this, "Menu"));
        }

        /// <summary>Occurs when navigation is requested.</summary>
        public event EventHandler<string> NavigationRequested;

        /// <summary>Gets the available themes.</summary>
        public ObservableCollection<string> AvailableThemes { get; } = new ObservableCollection<string> { "Default", "Dark", "Light", "Black&White" };

        /// <summary>Gets or sets the selected theme.</summary>
        public string SelectedTheme
        {
            get => this.selectedTheme;
            set => this.SetProperty(ref this.selectedTheme, value);
        }

        /// <summary>Gets or sets the selected language.</summary>
        public string SelectedLanguage
        {
            get => this.selectedLanguage;
            set => this.SetProperty(ref this.selectedLanguage, value);
        }

        /// <summary>Gets or sets a value indicating whether the piano is displayed at full width.</summary>
        public bool IsFullPianoWidth
        {
            get => this.isFullPianoWidth;
            set
            {
                if (this.SetProperty(ref this.isFullPianoWidth, value))
                {
                    this.IsDynamicPianoWidth = !value;
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether the piano width is dynamic.</summary>
        public bool IsDynamicPianoWidth
        {
            get => this.isDynamicPianoWidth;
            set
            {
                if (this.SetProperty(ref this.isDynamicPianoWidth, value))
                {
                    this.IsFullPianoWidth = !value;
                }
            }
        }

        /// <summary>Gets the available languages.</summary>
        public ObservableCollection<string> AvailableLanguages { get; } = new ObservableCollection<string> { "English", "Hungarian" };

        /// <summary>Gets the command to save the settings.</summary>
        public ICommand SaveSettingsCommand { get; }

        /// <summary>Gets the command to navigate back to the home screen.</summary>
        public ICommand GoHomeCommand { get; }

        private void SaveSettings()
        {
            this.ApplyTheme(this.SelectedTheme);

            Melody.UI.Properties.Settings.Default.Theme = this.SelectedTheme;
            Melody.UI.Properties.Settings.Default.IsFullPianoWidth = this.IsFullPianoWidth;
            Melody.UI.Properties.Settings.Default.Save();

            this.NavigationRequested?.Invoke(this, "Menu");
        }

        private void ApplyTheme(string themeName)
        {
            string themePath = $"Themes/{themeName}.xaml";

            try
            {
                System.Windows.ResourceDictionary newTheme = new System.Windows.ResourceDictionary
                {
                    Source = new Uri(themePath, UriKind.Relative),
                };

                System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(newTheme);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hiba a téma betöltésekor: {ex.Message}");
            }
        }
    }
}
