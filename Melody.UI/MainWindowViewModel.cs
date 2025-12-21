// Copyright (c) Matula Márton. All rights reserved.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Melody.Logic.Interfaces;
using NAudio.Midi;

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
    private IMxlUnpacker mxlUnpacker;
    private string svgSource;
    private bool isPianorollLoaded;
    private bool isSvgLoaded;
    private ObservableCollection<string> midiDevices;
    private int selectedMidiDeviceIndex;

    public MainWindowViewModel()
        : this(IsInDesignMode ? null : Ioc.Default.GetService<IToggleViewLogic>(), Ioc.Default.GetService<ILilypondLogic>(), Ioc.Default.GetService<IPianorollLogic>(), Ioc.Default.GetService<IMxlUnpacker>())
    {
    }

    public MainWindowViewModel(IToggleViewLogic toggleLogic, ILilypondLogic lilypondLogic, IPianorollLogic pianorollLogic, IMxlUnpacker mxlUnpacker)
    {
        this.IsActive = true;

        this.toggleLogic = toggleLogic;
        this.lilypondLogic = lilypondLogic;
        this.pianorollLogic = pianorollLogic;
        this.mxlUnpacker = mxlUnpacker;

        this.InitializeMidiDevices();

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
            if (this.openFileDialog.ShowDialog() == true)
            {
                string filePath = this.openFileDialog.FileName;
                this.lilypondLogic.LoadLilypond(filePath);
                this.svgSource = this.lilypondLogic.SvgPath;
                this.IsSvgLoaded = true;
            }
        });

        this.LoadPianorollCommand = new RelayCommand(() =>
        {
            if (this.openFileDialogPianoroll.ShowDialog() == true)
            {
                string filePath = this.openFileDialogPianoroll.FileName;
                this.pianorollLogic.InitializePianoRoll(filePath);
                this.IsPianorollLoaded = true;
            }
        });

        this.LoadSheetCommand = new RelayCommand(() =>
        {
            if (this.openFileDialog.ShowDialog() == true)
            {

                string filePath = this.openFileDialog.FileName;
                this.mxlUnpacker.ExtractAndSave(filePath, "extracted_musicxml.xml");
                this.lilypondLogic.LoadLilypond(this.mxlUnpacker.MxlPath);
                this.svgSource = this.lilypondLogic.SvgPath;
                this.pianorollLogic.InitializePianoRoll(this.mxlUnpacker.MusicXmlPath);
                this.IsPianorollLoaded = true; // TODO: parhuzamositas
                this.IsSvgLoaded = true; // TODO: parhuzamositas

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

    public ICommand LoadSheetCommand { get; set; }

    // Logic
    public IPianorollLogic PianorollLogic => this.pianorollLogic;

    // Properties
    public bool IsPianoRollView => this.toggleLogic.IsPianoRollView;

    public bool IsSheetMusicView => !this.toggleLogic.IsPianoRollView;

    public bool IsSvgLoaded
    {
        get => this.isSvgLoaded;
        set => this.SetProperty(ref this.isSvgLoaded, value);
    }

    public string ViewText => this.toggleLogic.IsPianoRollView ? "Piano roll" : "Sheet music";

    public string SvgSource
    {
        get => this.svgSource;
        set => this.SetProperty(ref this.svgSource, value);
    }

    public bool IsPianorollLoaded
    {
        get => this.isPianorollLoaded;
        set => this.SetProperty(ref this.isPianorollLoaded, value);
    }

    public ObservableCollection<string> MidiDevices => this.midiDevices;

    public int SelectedMidiDeviceIndex
    {
        get => this.selectedMidiDeviceIndex;
        set => this.SetProperty(ref this.selectedMidiDeviceIndex, value);
    }

    private void InitializeMidiDevices()
{
    this.midiDevices = new ObservableCollection<string>();
    for (int i = 0; i < MidiOut.NumberOfDevices; i++)
    {
        this.midiDevices.Add(MidiOut.DeviceInfo(i).ProductName);
    }

    this.selectedMidiDeviceIndex = this.midiDevices.Count > 0 ? 0 : -1;
}
}