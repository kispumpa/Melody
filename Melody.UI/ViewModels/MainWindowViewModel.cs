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

namespace Melody.UI.ViewModels
{
    public class MainWindowViewModel : ObservableRecipient
    {
        private readonly Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "MusicXML files (*.mxl)|*.mxl|All files (*.*)|*.*",
            Title = "Select a MusicXML file",
        };

        private IToggleViewLogic toggleLogic;
        private ILilypondLogic lilypondLogic;
        private IPianorollLogic pianorollLogic;
        private IMxlUnpacker mxlUnpacker;
        private ObservableCollection<string> imagePaths;
        private bool isPianorollLoaded;
        private bool isImageLoaded;
        private ObservableCollection<string> midiDevices;
        private int selectedMidiDeviceIndex;

        public MainWindowViewModel()
            : this(IsInDesignMode ? null : Ioc.Default.GetService<IToggleViewLogic>(), Ioc.Default.GetService<ILilypondLogic>(), Ioc.Default.GetService<IPianorollLogic>(), Ioc.Default.GetService<IMxlUnpacker>())
        {
        }

        public MainWindowViewModel(IToggleViewLogic toggleLogic, ILilypondLogic lilypondLogic, IPianorollLogic pianorollLogic, IMxlUnpacker mxlUnpacker)
        {
            IsActive = true;

            this.toggleLogic = toggleLogic;
            this.lilypondLogic = lilypondLogic;
            this.pianorollLogic = pianorollLogic;
            this.mxlUnpacker = mxlUnpacker;
            isPianorollLoaded = false;
            isImageLoaded = false;

            imagePaths = new ObservableCollection<string>();
            InitializeMidiDevices();

            Messenger.Register<MainWindowViewModel, string, string>(this, "ViewResult", (recipient, msg) =>
            {
                OnPropertyChanged(nameof(IsPianoRollView));
                OnPropertyChanged(nameof(IsSheetMusicView));
                OnPropertyChanged(nameof(ViewText));
                Debug.WriteLine(msg);
            });

            Messenger.Register<MainWindowViewModel, string, string>(this, "MusicXmlLoadResult", (recipient, msg) =>
            {
                if (msg.Contains("successfully"))
                {
                    isImageLoaded = true;
                    UpdateImagePaths();
                }
                OnPropertyChanged(nameof(IsImageLoaded));
                Debug.WriteLine(msg);
            });

            Messenger.Register<MainWindowViewModel, string, string>(this, "PianorollLoadResult", (recipient, msg) =>
            {
                Debug.WriteLine(msg);
            });

            ToggleViewCommand = new RelayCommand(() => this.toggleLogic.ToggleView());

            LoadSheetCommand = new RelayCommand(() =>
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    this.mxlUnpacker.ExtractAndSave(filePath, "extracted_musicxml.xml");
                    this.lilypondLogic.LoadLilypond(this.mxlUnpacker.MxlPath);
                    this.pianorollLogic.InitializePianoRoll(this.mxlUnpacker.MusicXmlPath);
                    IsPianorollLoaded = true;
                    IsImageLoaded = true;
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

        public ICommand LoadSheetCommand { get; set; }

        // Logic
        public IPianorollLogic PianorollLogic => pianorollLogic;

        // Properties
        public bool IsPianoRollView => toggleLogic.IsPianoRollView;

        public bool IsSheetMusicView => !toggleLogic.IsPianoRollView;

        public bool IsImageLoaded
        {
            get => isImageLoaded;
            set => SetProperty(ref isImageLoaded, value);
        }

        public string ViewText => toggleLogic.IsPianoRollView ? "Piano roll" : "Sheet music";

        public ObservableCollection<string> ImagePaths
        {
            get => imagePaths;
            set => SetProperty(ref imagePaths, value);
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

        private void UpdateImagePaths()
        {
            imagePaths.Clear();
            foreach (var path in lilypondLogic.GeneratedPngPaths)
            {
                imagePaths.Add(path);
            }
        }
    }
}
