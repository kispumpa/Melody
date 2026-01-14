using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Melody.Logic;
using Melody.Logic.Interfaces;
using Melody.Logic.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace Melody.UI.ViewModels
{
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
        private bool isPianorollLoaded;
        private string fileName;

        public PracticeWindowViewModel()
            : this(IsInDesignMode ? null : Ioc.Default.GetService<IPianorollLogic>(),
              Ioc.Default.GetService<IMxlUnpacker>(),
              Ioc.Default.GetService<IPracticeLogic>())
        {
        }

        public PracticeWindowViewModel(IPianorollLogic pianorollLogic, IMxlUnpacker mxlUnpacker, IPracticeLogic practiceLogic)
        {
            IsActive = true;
            IsPianorollLoaded = false;
            this.pianorollLogic = pianorollLogic;
            this.mxlUnpacker = mxlUnpacker;
            this.practiceLogic = practiceLogic;

            Messenger.Register<PracticeWindowViewModel, string, string>(this, "PianorollLoadResult", (recipient, msg) =>
            {
                Debug.WriteLine(msg);
            });

            Messenger.Register<PracticeWindowViewModel, string, string>(this, "PracticeLogicResult", (recipient, msg) =>
            {
                Debug.WriteLine(msg);
            });

            CreateSheetCommand = new RelayCommand(() =>
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    FileName = openFileDialog.SafeFileName;
                    this.mxlUnpacker.ExtractAndSave(filePath, $"{FileName}_musicxml.xml");
                    this.pianorollLogic.InitializePianoRoll(this.mxlUnpacker.MusicXmlPath);
                    IsPianorollLoaded = true;
                }
            });

            LoadSheetCommand = new RelayCommand(() =>
            {
                SheetCollection collection = FileController.GetCollection();
                SelectWindow selectWindow = new SelectWindow(collection.Sheets);
                string key = string.Empty;

                if ((bool)selectWindow.ShowDialog())
                {
                    key = selectWindow.SelectedKey;


                }
            });
        }

        public ICommand CreateSheetCommand { get; set; }

        public ICommand LoadSheetCommand { get; set; }

        public IPianorollLogic PianorollLogic => pianorollLogic;
        public IPracticeLogic PracticeLogic => practiceLogic;

        public string FileName
        {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        public bool IsPianorollLoaded
        {
            get => isPianorollLoaded;
            set => SetProperty(ref isPianorollLoaded, value);
        }

        public static bool IsInDesignMode
        {
            get
            {
                var prop = DesignerProperties.IsInDesignModeProperty;
                return (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
            }
        }
    }
}
