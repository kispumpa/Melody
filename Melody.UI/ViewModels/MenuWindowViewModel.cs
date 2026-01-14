using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Melody.UI.ViewModels
{
    public class MenuWindowViewModel : INotifyPropertyChanged
    {
        public ICommand StartPracticeCommand { get; }

        public ICommand PlayerCommand { get; set; }

        public ICommand SettingsCommand { get; }

        public ICommand ExitCommand { get; }

        public MenuWindowViewModel()
        {
            StartPracticeCommand = new RelayCommand(OnStartPractice);
            PlayerCommand = new RelayCommand(OnPlayer);
            SettingsCommand = new RelayCommand(OnSettings);
            ExitCommand = new RelayCommand(OnExit);
        }

        private void OnStartPractice()
        {
            var practiceWind = new PracticeWindow();
            practiceWind.Show();
        }

        private void OnSettings()
        {
            // Beállítások megnyitása
        }

        private void OnPlayer()
        {
            var mainWind = new MainWindow();
            mainWind.Show();
        }

        private void OnExit()
        {
            System.Windows.Application.Current.Shutdown();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
