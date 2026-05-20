// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI
{
    using System.Diagnostics;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Melody.Logic.Interfaces;
    using Melody.Logic.Models;
    using Melody.UI.ViewModels;
    using NAudio.Midi;

    /// <summary>Window for practicing piano rolls.</summary>
    public partial class PracticeWindow : UserControl
    {
        private const double PlaybackSpeed = 1.0;
        private const double PixelsPerSecond = 60;

        private PracticeWindowViewModel viewModel;
        private Canvas pianoKeysCanvas;
        private Canvas pianoRollCanvas;
        private Dictionary<Note, System.Windows.Shapes.Rectangle> noteRectangles;
        private object currentMeasureNumber;
        private double canvasHeight;
        private int currentMeasureIndex;
        private int allMeasure;
        private DateTime pianoRollStartTime;
        private double change;
        private bool isPianoRollInitialized = false;
        private bool isPianoRollPlaying = false;
        private bool isMidiInAvailable = false;
        private bool isWaiting = false;
        private double duration;
        private List<string> midiInDevices;
        private MidiIn midiIn;
        private List<int> pushedPitches;
        private List<int> waitingPitches;
        private Dictionary<string, int> nAudioNote = new Dictionary<string, int>
        {
            { "C", 0 },
            { "C#", 1 },
            { "D", 2 },
            { "D#", 3 },
            { "E", 4 },
            { "F", 5 },
            { "F#", 6 },
            { "G", 7 },
            { "G#", 8 },
            { "A", 9 },
            { "A#", 10 },
            { "B", 11 },
        };

        /// <summary>Initializes a new instance of the <see cref="PracticeWindow"/> class.</summary>
        public PracticeWindow()
        {
            this.InitializeComponent();

            this.DataContextChanged += this.PracticeWindow_DataContextChanged;

            this.noteRectangles = new Dictionary<Note, System.Windows.Shapes.Rectangle>();
            this.midiInDevices = new List<string>();
            this.pushedPitches = new List<int>();
            this.waitingPitches = new List<int>();

            this.Loaded += this.PracticeWindow_Loaded;
            this.Unloaded += this.PracticeWindow_Unloaded;
        }

        private void PracticeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("PracticeWindow loaded!");
            CompositionTarget.Rendering -= this.UpdateFrame;
            CompositionTarget.Rendering += this.UpdateFrame;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PracticeWindowViewModel.AreFilesLoaded))
            {
                if (this.viewModel.AreFilesLoaded)
                {
                    Debug.WriteLine("Loading piano roll practice with selected id...");

                    if (!this.viewModel.ToggleLogic.IsPianoRollView)
                    {
                        this.viewModel.ToggleLogic.ToggleView();
                    }

                    this.LoadPianoRoll();
                }
            }
            else if (e.PropertyName == nameof(PracticeWindowViewModel.IsPianorollLoaded))
            {
                if (this.viewModel.IsPianorollLoaded)
                {
                    if (!this.viewModel.ToggleLogic.IsPianoRollView)
                    {
                        this.viewModel.ToggleLogic.ToggleView();
                    }

                    this.InitializePianoRoll();
                }
            }
        }

        private void PracticeWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is PracticeWindowViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= this.ViewModel_PropertyChanged;
            }

            if (e.NewValue is PracticeWindowViewModel passedViewModel)
            {
                this.viewModel = passedViewModel;
                this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
            }
        }

        private void LoadPianoRoll()
        {
            this.pianorollGrid.Children.Clear();
            this.noteRectangles.Clear();

            IPianorollLogic logic = this.viewModel.PianorollLogic;

            this.pianoKeysCanvas = this.CreatePianoKeys(logic);
            this.pianorollGrid.Children.Add(this.pianoKeysCanvas);
            Grid.SetRow(this.pianoKeysCanvas, 1);

            this.pianoRollCanvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(200, 230, 255)),
                ClipToBounds = true,
            };
            this.pianorollGrid.Children.Add(this.pianoRollCanvas);
            Grid.SetRow(this.pianoRollCanvas, 0);

            this.UpdateLayout();
            this.canvasHeight = this.pianorollGrid.RowDefinitions[0].ActualHeight;

            this.currentMeasureIndex = this.viewModel.PracticeLogic.Progress.CurrentCombo;
            this.currentMeasureNumber = this.viewModel.PracticeLogic.Structure.Combos[this.currentMeasureIndex].MeasureNumber;
            this.CreateNextNotesInCanvas(this.viewModel.PracticeLogic);
            this.allMeasure = this.viewModel.PracticeLogic.Progress.TotalCombo;

            this.UpdateProgressText();

            this.btn_startPractice.IsEnabled = true;
            this.isPianoRollInitialized = true;
            Debug.WriteLine("Piano roll loaded for practice.");
        }

        private void IncreadeCurrentMeasure()
        {
            this.currentMeasureIndex++;
            if (this.currentMeasureIndex >= this.viewModel.PracticeLogic.Structure.Combos.Count)
            {
                this.currentMeasureIndex = this.viewModel.PracticeLogic.Structure.Combos.Count - 1;
            }

            this.currentMeasureNumber = this.viewModel.PracticeLogic.Structure.Combos[this.currentMeasureIndex].MeasureNumber;
        }

        private void CreateNextNotesInCanvas(IPracticeLogic logic)
        {
            this.noteRectangles.Clear();

            switch (this.currentMeasureNumber)
            {
                case JsonElement element:
                    switch (element.ValueKind)
                    {
                        case JsonValueKind.Number:
                            int measureNumber = element.GetInt32();
                            int phase = logic.Structure.Combos[this.currentMeasureIndex].Phase;

                            switch (phase)
                            {
                                case 0:
                                    this.SetChange("r", measureNumber);
                                    this.LoadMeasure("r", measureNumber);
                                    break;

                                case 1:
                                    this.SetChange("l", measureNumber);
                                    this.LoadMeasure("l", measureNumber);
                                    break;

                                case 2:
                                    this.SetChange("r", measureNumber);
                                    this.LoadMeasure("r", measureNumber);
                                    this.SetChange("l", measureNumber);
                                    this.LoadMeasure("l", measureNumber);
                                    break;

                                default:
                                    break;
                            }

                            break;

                        case JsonValueKind.String:
                            string measureString = element.GetString();
                            int measureMax = int.Parse(measureString.Substring(1));
                            int phaseA = logic.Structure.Combos[this.currentMeasureIndex].Phase;

                            switch (phaseA)
                            {
                                case 0:
                                    this.SetChange("r", 0);
                                    for (int i = 0; i <= measureMax; i++)
                                    {
                                        this.LoadMeasure("r", i);
                                    }

                                    break;

                                case 1:
                                    this.SetChange("l", 0);
                                    for (int i = 0; i <= measureMax; i++)
                                    {
                                        this.LoadMeasure("l", i);
                                    }

                                    break;

                                case 2:
                                    this.SetChange("r", 0);
                                    for (int i = 0; i <= measureMax; i++)
                                    {
                                        this.LoadMeasure("r", i);
                                    }

                                    this.SetChange("l", 0);
                                    for (int i = 0; i <= measureMax; i++)
                                    {
                                        this.LoadMeasure("l", i);
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
            Measure measure = this.viewModel.PracticeLogic.MeasureList.Measures.FirstOrDefault(m => m.ID == id);
            foreach (int number in measure.NoteNumbers)
            {
                Note note = this.viewModel.PracticeLogic.PracticeNotes[measureNumber][number];

                System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle
                {
                    Width = note.X.Length,
                    Height = note.Y.Length,
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)),
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 1,
                    Visibility = Visibility.Hidden,
                };
                note.Y.Position -= this.change;

                Canvas.SetLeft(rect, note.X.Position);
                this.pianoRollCanvas.Children.Add(rect);
                this.noteRectangles[note] = rect;
            }
        }

        private void SetChange(string side, int measureNumber)
        {
            string id = $"{side}{measureNumber}";
            Measure measure = this.viewModel.PracticeLogic.MeasureList.Measures.FirstOrDefault(m => m.ID == id);
            if (measure.NoteNumbers.Count == 0)
            {
            }

            int number = measure.NoteNumbers[0];
            Note note = this.viewModel.PracticeLogic.PracticeNotes[measureNumber][number];
            this.change = note.Y.Position - 300;
        }

        private void UpdatePianoRollFrame()
        {
            double elapsed = (DateTime.Now - this.pianoRollStartTime).TotalSeconds * PlaybackSpeed;

            foreach (KeyValuePair<Note, System.Windows.Shapes.Rectangle> kvp in this.noteRectangles)
            {
                Note note = kvp.Key;
                System.Windows.Shapes.Rectangle rect = kvp.Value;

                double y = note.Y.Position - (elapsed * PixelsPerSecond);
                bool isVisible = (y + note.Y.Length > 0) && (y < this.canvasHeight);

                if (isVisible)
                {
                    Canvas.SetBottom(rect, y);
                    rect.Visibility = Visibility.Visible;
                }
                else
                {
                    rect.Visibility = Visibility.Hidden;
                }

                if (!note.Played && y <= 0 && y > -note.Y.Length)
                {
                    this.PlayNote(note.Pitch, (int)note.Y.Length);
                    int midiNote = this.PitchToMidi(note.Pitch);
                    this.waitingPitches.Add(midiNote);
                    this.isWaiting = true;
                    note.Played = true;
                }
            }

            if (elapsed >= this.duration)
            {
                this.isPianoRollPlaying = false;
                this.btn_retry.IsEnabled = true;
                this.btn_continue.IsEnabled = true;
            }
        }

        private void PlayNote(string pitch, int length)
        {
            //int midiNote = PitchToMidi(pitch);

            //int durationMs = (int)((length / PixelsPerSecond) * 1000);

            //Task.Delay(durationMs).ContinueWith(_ =>
            //{
            //    waitingPitches.Remove(midiNote);
            //});
        }

        private int PitchToMidi(string pitch)
        {
            string step = pitch.Remove(pitch.Length - 1, 1);
            int octave = int.Parse(pitch.Substring(pitch.Length - 1, 1));
            int midiNote = (int)(MusicNote)Enum.Parse(typeof(MusicNote), step) + (12 * octave);
            return midiNote;
        }

        private void UpdateProgressText()
        {
            double prog = (double)this.currentMeasureIndex / (double)this.allMeasure * 100;
            this.practiceDisplay.Text = $"Progress: {(int)prog}%";
        }

        private Canvas CreatePianoKeys(IPianorollLogic logic)
        {
            Canvas canvas = new Canvas
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

                System.Windows.Shapes.Rectangle whiteKey = new System.Windows.Shapes.Rectangle
                {
                    Width = keyWidth,
                    Height = keyHeight,
                    Fill = System.Windows.Media.Brushes.White,
                    Stroke = System.Windows.Media.Brushes.Gray,
                    StrokeThickness = 1,
                };
                canvas.Children.Add(whiteKey);
                Canvas.SetLeft(whiteKey, keyWidth * i);

                if (hasBlackKey)
                {
                    System.Windows.Shapes.Rectangle blackKey = new System.Windows.Shapes.Rectangle
                    {
                        Width = keyWidth / 2,
                        Height = keyHeight / 2,
                        Fill = System.Windows.Media.Brushes.Black,
                        Stroke = System.Windows.Media.Brushes.Gray,
                        StrokeThickness = 1,
                    };
                    canvas.Children.Add(blackKey);
                    Canvas.SetLeft(blackKey, keyWidth * (i + 0.5));
                    Canvas.SetTop(blackKey, 0);
                }

                if (noteValue == 0)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = $"{(Step)noteValue}{logic.MinOctave + (i / 7)}",
                        FontSize = 11,
                        Foreground = System.Windows.Media.Brushes.Black,
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
            IPianorollLogic logic = this.viewModel.PianorollLogic;
            logic.StoreNotes(this.ActualWidth, isPractice: true);
            this.viewModel.PracticeLogic.CreatePractice(logic.PracticeNotes, this.viewModel.FileName, this.viewModel.PianorollLogic.TotalVisibleNotes, this.viewModel.PianorollLogic.MinOctave);
            Debug.WriteLine("Piano roll initialized for practice.");
            this.viewModel.PracticeLogic.LoadPractice();
            this.LoadPianoRoll();
        }

        private void UpdateFrame(object sender, EventArgs e)
        {
            //if (isPianoRollInitialized && isPianoRollPlaying  && pianoRollCanvas != null)
            if (this.isPianoRollInitialized && this.isPianoRollPlaying && !this.isWaiting && this.pianoRollCanvas != null)
            {
                this.UpdatePianoRollFrame();
            }

            if (!this.isMidiInAvailable)
            {
                for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                {
                    this.midiInDevices.Add(MidiIn.DeviceInfo(device).ProductName);
                }
            }

            if (!this.isMidiInAvailable && this.midiInDevices.Count > 0)
            {
                this.isMidiInAvailable = true;
                this.SetMidiIn();
            }

            if (this.isWaiting)
            {
                this.WaitingForMatch();
            }
        }

        private void WaitingForMatch()
        {
            bool egyezik = this.pushedPitches.Count == this.waitingPitches.Count &&
               this.pushedPitches.OrderBy(x => x).SequenceEqual(this.waitingPitches.OrderBy(x => x));
            if (egyezik && this.pushedPitches.Count != 0)
            {
                this.isWaiting = false;
                this.waitingPitches.Clear();
                this.pushedPitches.Clear();
            }
        }

        private void SetMidiIn()
        {
            this.midiInDevices.Add(MidiIn.DeviceInfo(0).ProductName);
            this.midiIn = new MidiIn(0);
            this.midiIn.MessageReceived += this.MidiIn_MessageReceived;

            this.midiConnectionDisplay.Text = $"Piano connected: {this.midiInDevices[0]}";
            this.midiConnectionDisplay.Foreground = (Brush)new BrushConverter().ConvertFrom("#27AE60");

            this.btn_createPractice.IsEnabled = true;
            this.btn_loadPractice.IsEnabled = true;
            this.midiIn.Start();
        }

        private void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            //if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOn)
            //{
            //    string message = e.MidiEvent.ToString();
            //    if (message.Contains("Len"))
            //    {
            //        string messagePitch = ExtractValue(message);
            //        int midiNote = NAudioPitchToMidi(messagePitch);
            //        Debug.WriteLine($"Note on received: {messagePitch} (MIDI note {midiNote})");
            //        pushedPitches.Add(midiNote);
            //    }
            //    else
            //    {
            //        string messagePitch = ExtractValue(message);
            //        int midiNote = NAudioPitchToMidi(messagePitch);
            //        pushedPitches.Remove(midiNote);
            //    }
            //}
            // Ellenőrizzük, hogy ez egy NoteOn típusú esemény-e
            if (e.MidiEvent is NAudio.Midi.NoteOnEvent noteOn)
            {
                int midiNote = noteOn.NoteNumber;

                // Ha a Velocity nagyobb mint 0, akkor a billentyűt Lenyomták
                if (noteOn.Velocity > 0)
                {
                    // Biztosítjuk, hogy ne kerüljön be duplán
                    if (!this.pushedPitches.Contains(midiNote))
                    {
                        this.pushedPitches.Add(midiNote);
                    }
                }

                // Ha a Velocity 0, az a billentyű Felengedését jelenti
                else
                {
                    this.pushedPitches.Remove(midiNote);
                }
            }

            // Kezeljük a dedikált NoteOff eseményt is (eszköze válogatja, melyiket küldi)
            else if (e.MidiEvent is NAudio.Midi.NoteEvent noteOff && e.MidiEvent.CommandCode == MidiCommandCode.NoteOff)
            {
                this.pushedPitches.Remove(noteOff.NoteNumber);
            }
        }

        private string ExtractValue(string text)
        {
            string pattern = @"Ch:\s+\d+\s+(?<ertek>.*?)\s+Vel";

            Match match = Regex.Match(text, pattern);

            if (match.Success)
            {
                return match.Groups["ertek"].Value.Trim();
            }

            return string.Empty;
        }

        private int NAudioPitchToMidi(string pitch)
        {
            string step = pitch.Remove(pitch.Length - 1, 1);
            int octave = int.Parse(pitch.Substring(pitch.Length - 1, 1));
            int midiNote = this.nAudioNote[step] + (12 * octave);
            return midiNote;
        }

        private void StartPracticeButton_Click(object sender, RoutedEventArgs e)
        {
            this.btn_startPractice.IsEnabled = false;
            this.btn_continue.IsEnabled = false;

            this.pianoRollStartTime = DateTime.Now;
            this.CalculateDuration();

            this.isPianoRollPlaying = true;
        }

        private void CalculateDuration()
        {
            this.duration = this.noteRectangles.Max(n => n.Key.Y.Position + n.Key.Y.Length) / PixelsPerSecond;
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            this.btn_startPractice.IsEnabled = false;
            this.btn_continue.IsEnabled = false;
            this.CreateNextNotesInCanvas(this.viewModel.PracticeLogic);
            this.pianoRollStartTime = DateTime.Now;
            this.isPianoRollPlaying = true;
            this.btn_retry.IsEnabled = false;
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            this.btn_continue.IsEnabled = false;
            this.btn_retry.IsEnabled = false;
            this.IncreadeCurrentMeasure();
            this.UpdateProgressText();
            this.CreateNextNotesInCanvas(this.viewModel.PracticeLogic);

            this.btn_startPractice.IsEnabled = true;
        }

        private void PracticeWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= this.UpdateFrame;
            //midiOut?.Dispose();
            this.isPianoRollInitialized = false;
            this.isPianoRollPlaying = false;
            this.pushedPitches.Clear();
            this.waitingPitches.Clear();

            if (this.viewModel != null)
            {
                this.viewModel.PropertyChanged -= this.ViewModel_PropertyChanged;
            }

            if (this.midiIn != null)
            {
                try
                {
                    this.midiIn.Stop();
                    this.midiIn.Dispose();
                }
                catch
                {
                }

                this.isMidiInAvailable = false;
            }

            if (this.viewModel.PracticeLogic.Progress != null)
            {
                this.viewModel.PracticeLogic.SaveProgress(this.currentMeasureIndex);
            }
        }
    }
}
