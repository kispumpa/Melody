namespace Melody.UI
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.Input;

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private IToggleViewLogic toggleLogic;

        public MainWindowViewModel()
        {
            this.toggleLogic = new ToggleViewLogic(); // nem jo
            this.toggleLogic.ViewChanged += this.ToggleLogic_ViewChanged;
            this.ToggleViewCommand = new RelayCommand(
                () => this.toggleLogic.ToggleView());
        }

        public event EventHandler ViewChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand ToggleViewCommand { get; set; }

        public bool IsPianoRollView
        {
            get => this.toggleLogic.IsPianoRollView;
        }

        public bool IsSheetMusicView
        {
            get => !this.toggleLogic.IsPianoRollView;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ToggleLogic_ViewChanged(object? sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(this.IsPianoRollView));
            this.OnPropertyChanged(nameof(this.IsSheetMusicView));
            this.ViewChanged?.Invoke(this, null);
        }
    }
}
