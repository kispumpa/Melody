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

        public PracticeWindowViewModel()
            : this(
                IsInDesignMode ? null : Ioc.Default.GetService<IPianorollLogic>(),
                Ioc.Default.GetService<IMxlUnpacker>(),
                Ioc.Default.GetService<IPracticeLogic>(),
                Ioc.Default.GetService<IToggleViewLogic>())
        {
        }

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

            GoHomeCommand = new RelayCommand(() => NavigationRequested?.Invoke(this, "Menu"));
        }

        public static bool IsInDesignMode
        {
            get
            {
                DependencyProperty prop = DesignerProperties.IsInDesignModeProperty;
                return (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
            }
        }

        public ICommand CreateSheetCommand { get; set; }

        public ICommand LoadSheetCommand { get; set; }

        public ICommand GoHomeCommand { get; set; }

        public event EventHandler<string> NavigationRequested;

        public bool IsPianoRollView => this.toggleLogic.IsPianoRollView;

        public bool IsButtonView => !this.toggleLogic.IsPianoRollView;

        public IPianorollLogic PianorollLogic => this.pianorollLogic;

        public IPracticeLogic PracticeLogic => this.practiceLogic;

        public IToggleViewLogic ToggleLogic => this.toggleLogic;

        public string FileName
        {
            get => this.fileName;
            set => this.SetProperty(ref this.fileName, value);
        }

        public bool IsPianorollLoaded
        {
            get => this.isPianorollLoaded;
            set => this.SetProperty(ref this.isPianorollLoaded, value);
        }

        public bool AreFilesLoaded
        {
            get => this.isKeySelected;
            set => this.SetProperty(ref this.isKeySelected, value);
        }
    }
}
