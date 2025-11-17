using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Melody.Logic.Interfaces;
using NAudio.Midi;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

public class MainWindowViewModel : ObservableRecipient
{
    private readonly Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "MusicXML files (*.mxl)|*.mxl|All files (*.*)|*.*",
        Title = "Select a MusicXML file",
    };

    private readonly Microsoft.Win32.OpenFileDialog openFileDialogPianoroll = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "Extracted MusicXML files (*.xml)|*.xml|All files (*.*)|*.*",
        Title = "Select an extracted MusicXML file",
    };

    private IToggleViewLogic toggleLogic;
    private ILilypondLogic lilypondLogic;
    private IPianorollLogic pianorollLogic;
    private string svgSource;
    private bool isPianorollLoaded;
    private ObservableCollection<string> midiDevices;
    private int selectedMidiDeviceIndex;

    public MainWindowViewModel()
        : this(IsInDesignMode ? null :
              Ioc.Default.GetService<IToggleViewLogic>(),
              Ioc.Default.GetService<ILilypondLogic>(),
              Ioc.Default.GetService<IPianorollLogic>())
    {
    }

    public MainWindowViewModel(IToggleViewLogic toggleLogic, ILilypondLogic lilypondLogic, IPianorollLogic pianorollLogic)
    {
        this.IsActive = true;

        this.toggleLogic = toggleLogic;
        this.lilypondLogic = lilypondLogic;
        this.pianorollLogic = pianorollLogic;

        InitializeMidiDevices();

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

        this.Messenger.Register<MainWindowViewModel, string, string>(this, "PianorollLoadResult", (recipient, msg) =>
        {
            Debug.WriteLine(msg);
        });

        this.ToggleViewCommand = new RelayCommand(() => this.toggleLogic.ToggleView());

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
                this.IsPianorollLoaded = true;
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

    // Commands
    public ICommand ToggleViewCommand { get; set; }
    public ICommand LoadLilypondCommand { get; set; }
    public ICommand LoadPianorollCommand { get; set; }

    // Logic
    public IPianorollLogic PianorollLogic => pianorollLogic;

    // Properties
    public bool IsPianoRollView => this.toggleLogic.IsPianoRollView;
    public bool IsSheetMusicView => !this.toggleLogic.IsPianoRollView;
    public bool IsSvgLoaded => !string.IsNullOrEmpty(this.svgSource);
    public string ViewText => this.toggleLogic.IsPianoRollView ? "Piano roll" : "Sheet music";

    public string SvgSource
    {
        get => svgSource;
        set => SetProperty(ref svgSource, value);
    }

    public bool IsPianorollLoaded
    {
        get => isPianorollLoaded;
        set => SetProperty(ref isPianorollLoaded, value);
    }

    public ObservableCollection<string> MidiDevices => midiDevices;

    public int SelectedMidiDeviceIndex
    {
        get => selectedMidiDeviceIndex;
        set => SetProperty(ref selectedMidiDeviceIndex, value);
    }

    private void InitializeMidiDevices()
    {
        midiDevices = new ObservableCollection<string>();
        for (int i = 0; i < MidiOut.NumberOfDevices; i++)
        {
            midiDevices.Add(MidiOut.DeviceInfo(i).ProductName);
        }
        selectedMidiDeviceIndex = midiDevices.Count > 0 ? 0 : -1;
    }
}