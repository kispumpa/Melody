using CommunityToolkit.Mvvm.DependencyInjection;
using Melody.Logic.Interfaces;
using Melody.Logic.Models;
using Melody.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Melody.UI
{
    /// <summary>
    /// Interaction logic for PracticeWindow.xaml
    /// </summary>
    public partial class PracticeWindow : Window
    {
        private const double PlaybackSpeed = 1.0;
        private const double PixelsPerSecond = 60;

        private PracticeWindowViewModel viewModel;
        private Canvas pianoKeysCanvas;
        private Canvas pianoRollCanvas;
        private Dictionary<Note, Rectangle> noteRectangles;
        private object currentMeasureNumber;
        private double canvasHeight;
        private int currentMeasureIndex;
        private DateTime pianoRollStartTime;
        private double change;

        public PracticeWindow()
        {
            InitializeComponent();
            this.viewModel = new ViewModels.PracticeWindowViewModel(Ioc.Default.GetService<IPianorollLogic>(),
              Ioc.Default.GetService<IMxlUnpacker>(),
              Ioc.Default.GetService<IPracticeLogic>(),
              Ioc.Default.GetService<IToggleViewLogic>());

            this.DataContext = this.viewModel;
            this.noteRectangles = new Dictionary<Note, Rectangle>();
            this.viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PracticeWindowViewModel.IsPianorollLoaded))
            {
                Debug.WriteLine("Initializing piano roll...");
                this.viewModel.ToggleLogic.ToggleView();
                this.InitializePianoRoll();
            }
            else if (e.PropertyName == nameof(PracticeWindowViewModel.AreFilesLoaded))
            {
                Debug.WriteLine("Loading piano roll practice with selected id...");
                this.viewModel.ToggleLogic.ToggleView();
                this.LoadPianoRoll();
            }
        }

        private void LoadPianoRoll()
        {
            var logic = this.viewModel.PianorollLogic;

            pianoKeysCanvas = CreatePianoKeys(logic);
            pianorollGrid.Children.Add(pianoKeysCanvas);
            Grid.SetRow(pianoKeysCanvas, 1);

            pianoRollCanvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(200, 230, 255)),
                ClipToBounds = true,
            };
            this.pianorollGrid.Children.Add(this.pianoRollCanvas);
            Grid.SetRow(this.pianoRollCanvas, 0);

            this.UpdateLayout();
            canvasHeight = pianorollGrid.RowDefinitions[0].ActualHeight;

            currentMeasureIndex = this.viewModel.PracticeLogic.Progress.CurrentCombo;
            currentMeasureNumber = this.viewModel.PracticeLogic.Structure.Combos[currentMeasureIndex].MeasureNumber;
            CreateNextNotesInCanvas(viewModel.PracticeLogic); // ehelyett a PracticeLogic-ból a következő egységet hívja meg
            pianoRollStartTime = DateTime.Now;
            UpdatePianoRollFrame();


            //isPianoRollPlaying = false;
            //isPianoRollInitialized = true;

            Debug.WriteLine("Piano roll loaded for practice.");
        }

        private void CreateNextNotesInCanvas(IPracticeLogic logic)
        {
            this.noteRectangles.Clear();

            switch (currentMeasureNumber)
            {
                case JsonElement element:
                    switch (element.ValueKind)
                    {
                        case JsonValueKind.Number:
                            int measureNumber = element.GetInt32();
                            int phase = logic.Structure.Combos[currentMeasureIndex].Phase;

                            switch (phase)
                            {
                                case 0:
                                    SetChange("r", measureNumber);
                                    LoadMeasure("r", measureNumber);
                                    break;

                                case 1:
                                    SetChange("l", measureNumber);
                                    LoadMeasure("l", measureNumber);
                                    break;

                                case 2:
                                    SetChange("r", measureNumber);
                                    LoadMeasure("r", measureNumber);
                                    SetChange("l", measureNumber);
                                    LoadMeasure("l", measureNumber);
                                    break;

                                default:
                                    break;
                            }

                            break;
                        case JsonValueKind.String:
                            break;
                        default:
                            break;
                    }

                    break;

                default:
                    break;
            }
            Debug.WriteLine($"Created {this.noteRectangles.Count} note rectangles");
        }

        private void LoadMeasure(string side, int measureNumber)
        {
            string id = $"{side}{measureNumber}";
            Measure measure = viewModel.PracticeLogic.MeasureList.Measures.FirstOrDefault(m => m.ID == id);
            foreach (var number in measure.NoteNumbers)
            {
                var note = viewModel.PracticeLogic.PracticeNotes[measureNumber][number];

                var rect = new Rectangle
                {
                    Width = note.X.Length,
                    Height = note.Y.Length,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Visibility = Visibility.Hidden,
                };
                note.Y.Position -= change;

                Canvas.SetLeft(rect, note.X.Position);
                this.pianoRollCanvas.Children.Add(rect);
                this.noteRectangles[note] = rect;
            }
        }

        private void SetChange(string side, int measureNumber)
        {
            string id = $"{side}{measureNumber}";
            Measure measure = viewModel.PracticeLogic.MeasureList.Measures.FirstOrDefault(m => m.ID == id);
            var number = measure.NoteNumbers[0];
            var note = viewModel.PracticeLogic.PracticeNotes[measureNumber][number];
            change = note.Y.Position - 300;
        }

        private void UpdatePianoRollFrame()
        {
            double elapsed = (DateTime.Now - pianoRollStartTime).TotalSeconds * PlaybackSpeed;

            foreach (var kvp in this.noteRectangles)
            {
                var note = kvp.Key;
                var rect = kvp.Value;

                double y = note.Y.Position - (elapsed * PixelsPerSecond);
                bool isVisible = (y + note.Y.Length > 0) && (y < canvasHeight);

                if (isVisible)
                {
                    Canvas.SetBottom(rect, y);
                    rect.Visibility = Visibility.Visible;
                }
                else
                {
                    rect.Visibility = Visibility.Hidden;
                }

                //if (!note.Played && y <= 0 && y > -note.Y.Length)
                //{
                //    this.PlayNote(note.Pitch, (int)note.Y.Length);
                //    note.Played = true;
                //}
            }

            //if (elapsed >= totalDuration)
            //{
            //    StopButton_Click(null, null);
            //}
        }

        private Canvas CreatePianoKeys(IPianorollLogic logic)
        {
            var canvas = new Canvas
            {
                Width = this.ActualWidth,
                Height = this.pianorollGrid.RowDefinitions[1].ActualHeight,
            };

            double keyWidth = this.ActualWidth / logic.TotalVisibleNotes;
            double keyHeight = this.pianorollGrid.RowDefinitions[1].ActualHeight;

            for (int i = 0; i < logic.TotalVisibleNotes; i++)
            {
                int noteValue = i % 7;
                bool hasBlackKey = noteValue == 0 || noteValue == 1 || noteValue == 3 || noteValue == 4 || noteValue == 5;

                var whiteKey = new Rectangle
                {
                    Width = keyWidth,
                    Height = keyHeight,
                    Fill = Brushes.White,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1,
                };
                canvas.Children.Add(whiteKey);
                Canvas.SetLeft(whiteKey, keyWidth * i);

                if (hasBlackKey)
                {
                    var blackKey = new Rectangle
                    {
                        Width = keyWidth / 2,
                        Height = keyHeight / 2,
                        Fill = Brushes.Black,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 1,
                    };
                    canvas.Children.Add(blackKey);
                    Canvas.SetLeft(blackKey, keyWidth * (i + 0.5));
                    Canvas.SetTop(blackKey, 0);
                }

                if (noteValue == 0)
                {
                    var label = new TextBlock
                    {
                        Text = $"{(Step)noteValue}{logic.MinOctave + (i / 7)}",
                        FontSize = 11,
                        Foreground = Brushes.Black,
                    };
                    canvas.Children.Add(label);
                    Canvas.SetLeft(label, (keyWidth * i) + 2);
                    Canvas.SetBottom(label, 2);
                }
            }

            return canvas;
        }

        private void InitializePianoRoll()
        {
            var logic = this.viewModel.PianorollLogic;
            logic.StoreNotes(this.ActualWidth, isPractice: true);
            this.viewModel.PracticeLogic.CreatePractice(logic.PracticeNotes, this.viewModel.FileName, viewModel.PianorollLogic.TotalVisibleNotes, viewModel.PianorollLogic.MinOctave);
            Debug.WriteLine("Piano roll initialized for practice.");
            this.viewModel.PracticeLogic.LoadPractice();
            LoadPianoRoll();
        }
    }
}
