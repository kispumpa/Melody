// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI.ViewModels
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using CommunityToolkit.Mvvm.Input;
    using Melody.Logic;
    using Melody.Logic.Interfaces;
    using Melody.Logic.Models;

    /// <summary>ViewModel for the practice window.</summary>
    public class PracticeWindowViewModel : ObservableRecipient
    {
        private readonly Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "MusicXML files (*.mxl)|*.mxl|All files (*.*)|*.*",
            Title = "Select a MusicXML file",
        };

        private IPianorollLogic pianorollLogic;
        private IMxlUnpacker mxlUnpacker;
        private IPracticeLogic practiceLogic;
        private IToggleViewLogic toggleLogic;
        private bool isPianorollLoaded;
        private bool isKeySelected;
        private string fileName;

        /// <summary>Initializes a new instance of the <see cref="PracticeWindowViewModel"/> class.</summary>
        public PracticeWindowViewModel()
            : this(
                IsInDesignMode ? null : Ioc.Default.GetService<IPianorollLogic>(),
                Ioc.Default.GetService<IMxlUnpacker>(),
                Ioc.Default.GetService<IPracticeLogic>(),
                Ioc.Default.GetService<IToggleViewLogic>())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="PracticeWindowViewModel"/> class with the specified logic components.</summary>
        /// <param name="pianorollLogic">The pianoroll logic component.</param>
        /// <param name="mxlUnpacker">The MusicXML unpacker component.</param>
        /// <param name="practiceLogic">The practice logic component.</param>
        /// <param name="toggleLogic">The toggle view logic component.</param>
        public PracticeWindowViewModel(IPianorollLogic pianorollLogic, IMxlUnpacker mxlUnpacker, IPracticeLogic practiceLogic, IToggleViewLogic toggleLogic)
        {
            this.IsActive = true;
            this.IsPianorollLoaded = false;
            this.pianorollLogic = pianorollLogic;
            this.mxlUnpacker = mxlUnpacker;
            this.practiceLogic = practiceLogic;
            this.toggleLogic = toggleLogic;

            this.Messenger.Register<PracticeWindowViewModel, string, string>(this, "ViewResult", (recipient, msg) =>
            {
                this.OnPropertyChanged(nameof(this.IsPianoRollView));
                this.OnPropertyChanged(nameof(this.IsButtonView));
                Debug.WriteLine(msg);
            });

            this.Messenger.Register<PracticeWindowViewModel, string, string>(this, "PianorollLoadResult", (recipient, msg) =>
            {
                Debug.WriteLine(msg);
            });

            this.Messenger.Register<PracticeWindowViewModel, string, string>(this, "PracticeLogicResult", (recipient, msg) =>
            {
                Debug.WriteLine(msg);
            });

            this.CreateSheetCommand = new RelayCommand(() =>
            {
                this.isPianorollLoaded = false;

                if (this.openFileDialog.ShowDialog() == true)
                {
                    string filePath = this.openFileDialog.FileName;
                    this.FileName = this.openFileDialog.SafeFileName;
                    this.mxlUnpacker.ExtractAndSave(filePath, $"{this.FileName}_musicxml.xml");
                    this.pianorollLogic.InitializePianoRoll(this.mxlUnpacker.MusicXmlPath);
                    this.IsPianorollLoaded = true;
                }
            });

            this.LoadSheetCommand = new RelayCommand(() =>
            {
                this.AreFilesLoaded = false;

                SheetCollection collection = FileController.GetCollection();
                SelectWindow selectWindow = new SelectWindow(collection.Sheets);
                string key = string.Empty;

                if ((bool)selectWindow.ShowDialog())
                {
                    key = selectWindow.SelectedKey;
                    this.practiceLogic.LoadPractice(key);
                    this.pianorollLogic.TotalVisibleNotes = this.practiceLogic.Progress.TotalVisibleNotes;
                    this.pianorollLogic.MinOctave = this.practiceLogic.Progress.MinOctave;
                    this.AreFilesLoaded = true;
                }
            });

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

        /// <summary>Gets or sets the command to create a new sheet.</summary>
        public ICommand CreateSheetCommand { get; set; }

        /// <summary>Gets or sets the command to load an existing sheet.</summary>
        public ICommand LoadSheetCommand { get; set; }

        /// <summary>Gets or sets the command to navigate to the home view.</summary>
        public ICommand GoHomeCommand { get; set; }

        /// <summary>Gets a value indicating whether the piano roll view is active.</summary>
        public bool IsPianoRollView => this.toggleLogic.IsPianoRollView;

        /// <summary>Gets a value indicating whether the button view is active.</summary>
        public bool IsButtonView => !this.toggleLogic.IsPianoRollView;

        /// <summary>Gets the pianoroll logic component.</summary>
        public IPianorollLogic PianorollLogic => this.pianorollLogic;

        /// <summary>Gets the practice logic component.</summary>
        public IPracticeLogic PracticeLogic => this.practiceLogic;

        /// <summary>Gets the toggle view logic component.</summary>
        public IToggleViewLogic ToggleLogic => this.toggleLogic;

        /// <summary>Gets or sets the name of the currently loaded file.</summary>
        public string FileName
        {
            get => this.fileName;
            set => this.SetProperty(ref this.fileName, value);
        }

        /// <summary>Gets or sets a value indicating whether the piano roll view is loaded.</summary>
        public bool IsPianorollLoaded
        {
            get => this.isPianorollLoaded;
            set => this.SetProperty(ref this.isPianorollLoaded, value);
        }

        /// <summary>Gets or sets a value indicating whether the necessary files for practice are loaded.</summary>
        public bool AreFilesLoaded
        {
            get => this.isKeySelected;
            set => this.SetProperty(ref this.isKeySelected, value);
        }
    }
}
