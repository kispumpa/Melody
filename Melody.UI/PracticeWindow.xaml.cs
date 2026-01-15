using CommunityToolkit.Mvvm.DependencyInjection;
using Melody.Logic.Interfaces;
using Melody.Logic.Models;
using Melody.UI.ViewModels;
using NAudio.Midi;
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
        private int allMeasure;
        private DateTime pianoRollStartTime;
        private double change;
        private bool isPianoRollInitialized = false;
        private bool isPianoRollPlaying = false;
        private double duration;

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

            CompositionTarget.Rendering += UpdateFrame;
            this.Closing += PracticeWindow_Closing;
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
            CreateNextNotesInCanvas(viewModel.PracticeLogic);
            allMeasure = this.viewModel.PracticeLogic.Progress.TotalCombo;

            UpdateProgressText();

            btn_startPractice.IsEnabled = true;
            isPianoRollInitialized = true;


            Debug.WriteLine("Piano roll loaded for practice.");
        }

        private void IncreadeCurrentMeasure()
        {
            currentMeasureIndex++;
            if (currentMeasureIndex >= this.viewModel.PracticeLogic.Structure.Combos.Count)
            {
                currentMeasureIndex = this.viewModel.PracticeLogic.Structure.Combos.Count - 1;
            }

            currentMeasureNumber = this.viewModel.PracticeLogic.Structure.Combos[currentMeasureIndex].MeasureNumber;
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
                            string measureString = element.GetString();
                            int measureMax = int.Parse(measureString.Substring(1));
                            int phaseA = logic.Structure.Combos[currentMeasureIndex].Phase;

                            switch (phaseA)
                            {
                                case 0:
                                    SetChange("r", 0);
                                    for (int i = 0; i <= measureMax; i++)
                                    {
                                        LoadMeasure("r", i);
                                    }

                                    break;

                                case 1:
                                    SetChange("l", 0);
                                    for (int i = 0; i <= measureMax; i++)
                                    {
                                        LoadMeasure("l", i);
                                    }

                                    break;

                                case 2:
                                    SetChange("r", 0);
                                    for (int i = 0; i <= measureMax; i++)
                                    {
                                        LoadMeasure("r", i);
                                    }

                                    SetChange("l", 0);
                                    for (int i = 0; i <= measureMax; i++)
                                    {
                                        LoadMeasure("l", i);
                                    }

                                    break;

                                default:
                                    break;
                            }
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
            if (measure.NoteNumbers.Count == 0)
            {

            }
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

            if (elapsed >= duration)
            {
                isPianoRollPlaying = false;
                btn_retry.IsEnabled = true;
                btn_continue.IsEnabled = true;
            }
        }

        private void UpdateProgressText()
        {
            var prog = (double)currentMeasureIndex / (double)allMeasure * 100;
            practiceDisplay.Text = $"Progress: {(int)prog}%";
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

        private void UpdateFrame(object sender, EventArgs e)
        {
            if (isPianoRollInitialized && isPianoRollPlaying && pianoRollCanvas != null)
            {
                UpdatePianoRollFrame();
            }
        }

        private void StartPracticeButton_Click(object sender, RoutedEventArgs e)
        {
            btn_startPractice.IsEnabled = false;
            btn_continue.IsEnabled = false;

            pianoRollStartTime = DateTime.Now;
            CalculateDuration();

            isPianoRollPlaying = true;
        }

        private void CalculateDuration()
        {
            duration = noteRectangles.Max(n => n.Key.Y.Position + n.Key.Y.Length) / PixelsPerSecond;
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            btn_startPractice.IsEnabled = false;
            btn_continue.IsEnabled = false;
            CreateNextNotesInCanvas(viewModel.PracticeLogic);
            pianoRollStartTime = DateTime.Now;
            isPianoRollPlaying = true;
            btn_retry.IsEnabled = false;
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            btn_continue.IsEnabled = false;
            btn_retry.IsEnabled = false;
            IncreadeCurrentMeasure();
            UpdateProgressText();
            CreateNextNotesInCanvas(viewModel.PracticeLogic);

            btn_startPractice.IsEnabled = true;
        }

        private void PracticeWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= UpdateFrame;
            //midiOut?.Dispose();
            isPianoRollInitialized = false;
            isPianoRollPlaying = false;

            this.viewModel.ToggleLogic.ToggleView();

            viewModel.PracticeLogic.SaveProgress(currentMeasureIndex);
        }
    }
}
