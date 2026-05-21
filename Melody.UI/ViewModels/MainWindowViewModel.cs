// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI.ViewModels
{
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

    /// <summary>Main window view model.</summary>
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

        private bool isLoading = false;
        private ObservableCollection<string> loadingMessages;

        /// <summary>Initializes a new instance of the <see cref="MainWindowViewModel"/> class.</summary>
        public MainWindowViewModel()
            : this(IsInDesignMode ? null : Ioc.Default.GetService<IToggleViewLogic>(), Ioc.Default.GetService<ILilypondLogic>(), Ioc.Default.GetService<IPianorollLogic>(), Ioc.Default.GetService<IMxlUnpacker>())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="MainWindowViewModel"/> class with the specified logic components.</summary>
        /// <param name="toggleLogic">The toggle view logic.</param>
        /// <param name="lilypondLogic">The Lilypond logic.</param>
        /// <param name="pianorollLogic">The pianoroll logic.</param>
        /// <param name="mxlUnpacker">The MXL unpacker.</param>
        public MainWindowViewModel(IToggleViewLogic toggleLogic, ILilypondLogic lilypondLogic, IPianorollLogic pianorollLogic, IMxlUnpacker mxlUnpacker)
        {
            this.IsActive = true;

            this.toggleLogic = toggleLogic;
            this.lilypondLogic = lilypondLogic;
            this.pianorollLogic = pianorollLogic;
            this.mxlUnpacker = mxlUnpacker;
            this.isPianorollLoaded = false;
            this.isImageLoaded = false;
            this.loadingMessages = new ObservableCollection<string>();

            this.imagePaths = new ObservableCollection<string>();
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (msg.Contains("successfully"))
                    {
                        recipient.IsImageLoaded = true;
                        recipient.UpdateImagePaths();
                    }

                    recipient.OnPropertyChanged(nameof(this.IsImageLoaded));

                    recipient.LoadingMessages.Add($"[Sheet]: {msg}");
                });
                Debug.WriteLine(msg);
            });

            this.Messenger.Register<MainWindowViewModel, string, string>(this, "PianorollLoadResult", (recipient, msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    recipient.LoadingMessages.Add($"[PianoRoll]: {msg}");
                });

                Debug.WriteLine(msg);
            });

            this.ToggleViewCommand = new RelayCommand(() => this.toggleLogic.ToggleView());

            this.LoadSheetCommand = new AsyncRelayCommand(this.LoadSheetAsync);

            this.GoHomeCommand = new RelayCommand(() => this.NavigationRequested?.Invoke(this, "Menu"));
        }

        /// <summary>Occurs when navigation is requested.</summary>
        public event EventHandler<string> NavigationRequested;

        /// <summary>Gets a value indicating whether the application is in design mode.</summary>
        public static bool IsInDesignMode
        {
            get
            {
                DependencyProperty prop = DesignerProperties.IsInDesignModeProperty;
                return (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
            }
        }

        /// <summary>Gets or sets the command to toggle the view.</summary>
        public ICommand ToggleViewCommand { get; set; }

        /// <summary>Gets or sets the command to load a sheet.</summary>
        public ICommand LoadSheetCommand { get; set; }

        /// <summary>Gets or sets the command to navigate to the home view.</summary>
        public ICommand GoHomeCommand { get; set; }

        /// <summary>Gets the pianoroll logic.</summary>
        public IPianorollLogic PianorollLogic => this.pianorollLogic;

        /// <summary>Gets a value indicating whether the piano roll view is active.</summary>
        public bool IsPianoRollView => this.toggleLogic.IsPianoRollView;

        /// <summary>Gets a value indicating whether the sheet music view is active.</summary>
        public bool IsSheetMusicView => !this.toggleLogic.IsPianoRollView;

        /// <summary>Gets or sets a value indicating whether the application is loading.</summary>
        public bool IsLoading
        {
            get => this.isLoading;
            set => this.SetProperty(ref this.isLoading, value);
        }

        /// <summary>Gets or sets the collection of loading messages.</summary>
        public ObservableCollection<string> LoadingMessages
        {
            get => this.loadingMessages;
            set => this.SetProperty(ref this.loadingMessages, value);
        }

        /// <summary>Gets or sets a value indicating whether the image is loaded.</summary>
        public bool IsImageLoaded
        {
            get => this.isImageLoaded; set => this.SetProperty(ref this.isImageLoaded, value);
        }

        /// <summary>Gets the text representing the current view.</summary>
        public string ViewText => this.toggleLogic.IsPianoRollView ? "Piano roll" : "Sheet music";

        /// <summary>Gets or sets the collection of image paths.</summary>
        public ObservableCollection<string> ImagePaths
        {
            get => this.imagePaths; set => this.SetProperty(ref this.imagePaths, value);
        }

        /// <summary>Gets or sets a value indicating whether the piano roll is loaded.</summary>
        public bool IsPianorollLoaded
        {
            get => this.isPianorollLoaded; set => this.SetProperty(ref this.isPianorollLoaded, value);
        }

        /// <summary>Gets the collection of available MIDI devices.</summary>
        public ObservableCollection<string> MidiDevices => this.midiDevices;

        /// <summary>Gets or sets the index of the selected MIDI device.</summary>
        public int SelectedMidiDeviceIndex
        {
            get => this.selectedMidiDeviceIndex; set => this.SetProperty(ref this.selectedMidiDeviceIndex, value);
        }

        private async Task LoadSheetAsync()
        {
            if (this.openFileDialog.ShowDialog() == true)
            {
                string filePath = this.openFileDialog.FileName;

                this.IsLoading = true;
                this.LoadingMessages.Clear();

                // Messenger.Send("Fájl betöltése megkezdődött...", "LogMessage");
                this.IsPianorollLoaded = false;
                this.IsImageLoaded = false;
                try
                {
                    await Task.Run(() =>
                    {
                        // Messenger.Send("MusicXML kicsomagolása...", "LogMessage");
                        this.mxlUnpacker.ExtractAndSave(filePath, "extracted_musicxml.xml");

                        // Messenger.Send("Kották generálása Lilypond segítségével...", "LogMessage");
                        this.lilypondLogic.LoadLilypond(this.mxlUnpacker.MxlPath);

                        // Messenger.Send("Zongoratekercs inicializálása...", "LogMessage");
                        this.pianorollLogic.InitializePianoRoll(this.mxlUnpacker.MusicXmlPath, Properties.Settings.Default.IsFullPianoWidth);
                    });

                    this.IsPianorollLoaded = true;
                    this.IsImageLoaded = true;

                    // Messenger.Send("Sikeresen befejeződött!", "LogMessage");
                    await Task.Delay(1000);
                }
                catch (Exception)
                {
                    // Messenger.Send($"Hiba történt: {ex.Message}", "LogMessage");
                    await Task.Delay(3000);
                }
                finally
                {
                    this.IsLoading = false;
                }
            }
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

        private void UpdateImagePaths()
        {
            this.imagePaths.Clear();
            foreach (string path in this.lilypondLogic.GeneratedPngPaths)
            {
                this.imagePaths.Add(path);
            }
        }
    }
}
