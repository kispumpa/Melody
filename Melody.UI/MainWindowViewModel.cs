namespace Melody.UI
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using CommunityToolkit.Mvvm.Input;

    public class MainWindowViewModel : ObservableRecipient
    {
        private IToggleViewLogic toggleLogic;

        public MainWindowViewModel()
            : this(IsInDesignMode ? null : Ioc.Default.GetService<IToggleViewLogic>())
        {
        }

        public MainWindowViewModel(IToggleViewLogic toggleLogic)
        {
            this.toggleLogic = toggleLogic;
            this.Messenger.Register<MainWindowViewModel, string, string>(this, "ViewResult", (recipient, msg) =>
            {
                this.OnPropertyChanged(nameof(this.IsPianoRollView));
                this.OnPropertyChanged(nameof(this.IsSheetMusicView));
            });

            this.ToggleViewCommand = new RelayCommand(
                () => this.toggleLogic.ToggleView());
        }

        public static bool IsInDesignMode
        {
            get
            {
                var prop = DesignerProperties.IsInDesignModeProperty;
                return (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
            }
        }

        public ICommand ToggleViewCommand { get; set; }

        public bool IsPianoRollView
        {
            get => this.toggleLogic.IsPianoRollView;
        }

        public bool IsSheetMusicView
        {
            get => !this.toggleLogic.IsPianoRollView;
        }
    }
}
