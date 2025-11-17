namespace Melody.UI
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using CommunityToolkit.Mvvm.Input;
    using Melody.Logic.Interfaces;
    using Microsoft.Win32;

    public class MainWindowViewModel : ObservableRecipient
    {
        private readonly OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "MusicXML files (*.mxl)|*.mxl|All files (*.*)|*.*",
            Title = "Select a MusicXML file",
        };

        private readonly OpenFileDialog openFileDialogPianoroll = new OpenFileDialog
        {
            Filter = "Extracted MusicXML files (*.xml)|*.xml|All files (*.*)|*.*",
            Title = "Select an extracted MusicXML file",
        };

        private IToggleViewLogic toggleLogic;
        private ILilypondLogic lilypondLogic;
        private IPianorollLogic pianorollLogic;
        private string svgSource;

        // constructors
        public MainWindowViewModel()
            : this(IsInDesignMode ? null : 
                  Ioc.Default.GetService<IToggleViewLogic>(), 
                  Ioc.Default.GetService<ILilypondLogic>(),
                  Ioc.Default.GetService<IPianorollLogic>())
        {
        }

        public MainWindowViewModel(IToggleViewLogic toggleLogic, ILilypondLogic lilypondLogic, IPianorollLogic pianorollLogic)
        {
            this.toggleLogic = toggleLogic;
            this.lilypondLogic = lilypondLogic;
            this.pianorollLogic = pianorollLogic;

            this.Messenger.Register<MainWindowViewModel, string, string>(this, "ViewResult", (recipient, msg) =>
            {
                this.OnPropertyChanged(nameof(this.IsPianoRollView));
                this.OnPropertyChanged(nameof(this.IsSheetMusicView));
                this.OnPropertyChanged(nameof(this.ViewText));
                Debug.WriteLine(msg);
            });
            this.Messenger.Register<MainWindowViewModel, string, string>(this, "MusicXmlLoadResult", (recipient, msg) =>
            {
                this.OnPropertyChanged(nameof(this.IsSvgLoaded));
                Debug.WriteLine(msg);
            });

            this.ToggleViewCommand = new RelayCommand(
                () => this.toggleLogic.ToggleView());
            this.LoadLilypondCommand = new RelayCommand(() =>
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    this.lilypondLogic.LoadLilypond(filePath);
                    this.svgSource = this.lilypondLogic.SvgPath;
                }
            });
            this.LoadPianorollCommand = new RelayCommand(() =>
            {
                if (openFileDialogPianoroll.ShowDialog() == true)
                {
                    string filePath = openFileDialogPianoroll.FileName;
                    this.pianorollLogic.LoadPianoroll(filePath);
                }
            });
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

        public ICommand LoadLilypondCommand { get; set; }

        public ICommand LoadPianorollCommand { get; set; }

        // Properties
        public bool IsPianoRollView
        {
            get => this.toggleLogic.IsPianoRollView;
        }

        public bool IsSheetMusicView
        {
            get => !this.toggleLogic.IsPianoRollView;
        }

        public bool IsSvgLoaded
        {
            get => !string.IsNullOrEmpty(this.svgSource);
        }

        public string ViewText
        {
            get => this.toggleLogic.IsPianoRollView ? "Piano roll" : "Sheet music";
        }

        public string SvgSource
        {
            get => this.svgSource;
            set
            {
                if (this.svgSource != value)
                {
                    this.svgSource = value;
                    this.OnPropertyChanged(); // kulon kell
                }
            }
        }
    }
}
