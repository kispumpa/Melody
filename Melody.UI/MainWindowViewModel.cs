namespace Melody.UI
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using CommunityToolkit.Mvvm.Input;
    using Melody.Logic;

    public class MainWindowViewModel : ObservableRecipient
    {
        private IToggleViewLogic toggleLogic;
        private IMusicXmlLogic musicXmlLogic;

    // constructors
        public MainWindowViewModel()
            : this(IsInDesignMode ? null : Ioc.Default.GetService<IToggleViewLogic>(), Ioc.Default.GetService<IMusicXmlLogic>())
        {
        }

        public MainWindowViewModel(IToggleViewLogic toggleLogic, IMusicXmlLogic musicXmlLogic)
        {
            this.toggleLogic = toggleLogic;
            this.musicXmlLogic = musicXmlLogic;

            this.Messenger.Register<MainWindowViewModel, string, string>(this, "ViewResult", (recipient, msg) =>
            {
                this.OnPropertyChanged(nameof(this.IsPianoRollView));
                this.OnPropertyChanged(nameof(this.IsSheetMusicView));
                Debug.WriteLine(msg);
            });
            this.Messenger.Register<MainWindowViewModel, string, string>(this, "MusicXmlLoadResult", (recipient, msg) =>
            {
                Debug.WriteLine(msg);
            });

            this.ToggleViewCommand = new RelayCommand(
                () => this.toggleLogic.ToggleView());
            this.LoadMusicXmlCommand = new RelayCommand(
                () => this.musicXmlLogic.LoadMusicXml());
        }

        public static bool IsInDesignMode
        {
            get
            {
                var prop = DesignerProperties.IsInDesignModeProperty;
                return (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
            }
        }

    // ICommands
        public ICommand ToggleViewCommand { get; set; }

        public ICommand LoadMusicXmlCommand { get; set; }

    // Properties
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
