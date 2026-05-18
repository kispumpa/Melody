using Melody.Logic.Interfaces;
using Melody.Logic.Models;
using Melody.UI.ViewModels;
using NAudio.Midi;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Melody.UI
{
    /// <summary>
    /// Interaction logic for PracticeWindow.xaml
    /// </summary>
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
        private Dictionary<string, int> NAudioNote = new Dictionary<string, int>
        {
            {"C", 0 },
            {"C#", 1 },
            {"D", 2 },
            {"D#", 3 },
            {"E", 4 },
            {"F", 5 },
            {"F#", 6 },
            {"G", 7 },
            {"G#", 8 },
            {"A", 9 },
            {"A#", 10 },
            {"B", 11 },
        };

        public PracticeWindow()
        {
            InitializeComponent();
            //this.viewModel = new ViewModels.PracticeWindowViewModel(Ioc.Default.GetService<IPianorollLogic>(),
            //  Ioc.Default.GetService<IMxlUnpacker>(),
            //  Ioc.Default.GetService<IPracticeLogic>(),
            //  Ioc.Default.GetService<IToggleViewLogic>());

            //this.DataContext = this.viewModel;
            this.DataContextChanged += PracticeWindow_DataContextChanged;

            this.noteRectangles = new Dictionary<Note, System.Windows.Shapes.Rectangle>();
            midiInDevices = new List<string>();
            pushedPitches = new List<int>();
            waitingPitches = new List<int>();

            //this.viewModel.PropertyChanged += ViewModel_PropertyChanged;
            //CompositionTarget.Rendering += UpdateFrame;
            this.Loaded += this.PracticeWindow_Loaded;
            this.Unloaded += PracticeWindow_Unloaded;
        }

        private void PracticeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("PracticeWindow loaded!");
            CompositionTarget.Rendering -= UpdateFrame;
            CompositionTarget.Rendering += UpdateFrame;

            if (this.viewModel.AreFilesLoaded && !this.isPianoRollInitialized)
            {
                Debug.WriteLine("Restoring existing practice piano roll...");
                LoadPianoRoll();
            }
            else if (this.viewModel.IsPianorollLoaded && !this.isPianoRollInitialized)
            {
                Debug.WriteLine("Re-initializing practice piano roll...");
                InitializePianoRoll();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == nameof(PracticeWindowViewModel.IsPianorollLoaded))
            //{
            //    Debug.WriteLine("Initializing piano roll...");
            //    this.viewModel.ToggleLogic.ToggleView();
            //    this.InitializePianoRoll();
            //}
            //else if (e.PropertyName == nameof(PracticeWindowViewModel.AreFilesLoaded))
            //{
            //    Debug.WriteLine("Loading piano roll practice with selected id...");
            //    this.viewModel.ToggleLogic.ToggleView();
            //    this.LoadPianoRoll();
            //}
            if (e.PropertyName == nameof(PracticeWindowViewModel.AreFilesLoaded))
            {
                // Csak akkor töltsünk be, ha az érték IGAZ lett (és nem false-ra állítottuk vissza)
                if (this.viewModel.AreFilesLoaded)
                {
                    Debug.WriteLine("Loading piano roll practice with selected id...");

                    // Csak akkor váltsunk nézetet, ha épp a gombokat látjuk!
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
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue is PracticeWindowViewModel passedViewModel)
            {
                this.viewModel = passedViewModel;

                this.viewModel.PropertyChanged += ViewModel_PropertyChanged;

                if (this.IsLoaded)
                {
                    if (this.viewModel.AreFilesLoaded && !this.isPianoRollInitialized)
                    {
                        LoadPianoRoll();
                    }
                    else if (this.viewModel.IsPianorollLoaded && !this.isPianoRollInitialized)
                    {
                        InitializePianoRoll();
                    }
                }
            }
        }

        private void LoadPianoRoll()
        {
            pianorollGrid.Children.Clear();
            noteRectangles.Clear();

            IPianorollLogic logic = this.viewModel.PianorollLogic;

            pianoKeysCanvas = CreatePianoKeys(logic);
            pianorollGrid.Children.Add(pianoKeysCanvas);
            Grid.SetRow(pianoKeysCanvas, 1);

            pianoRollCanvas = new Canvas
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 230, 255)),
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
            foreach (int number in measure.NoteNumbers)
            {
                Note note = viewModel.PracticeLogic.PracticeNotes[measureNumber][number];

                System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle
                {
                    Width = note.X.Length,
                    Height = note.Y.Length,
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)),
                    Stroke = System.Windows.Media.Brushes.Black,
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
            int number = measure.NoteNumbers[0];
            Note note = viewModel.PracticeLogic.PracticeNotes[measureNumber][number];
            change = note.Y.Position - 300;
        }

        private void UpdatePianoRollFrame()
        {
            double elapsed = (DateTime.Now - pianoRollStartTime).TotalSeconds * PlaybackSpeed;

            foreach (KeyValuePair<Note, System.Windows.Shapes.Rectangle> kvp in this.noteRectangles)
            {
                Note note = kvp.Key;
                System.Windows.Shapes.Rectangle rect = kvp.Value;

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

                if (!note.Played && y <= 0 && y > -note.Y.Length)
                {
                    PlayNote(note.Pitch, (int)note.Y.Length);
                    int midiNote = PitchToMidi(note.Pitch);
                    waitingPitches.Add(midiNote);
                    isWaiting = true;
                    note.Played = true;
                }
            }

            if (elapsed >= duration)
            {
                isPianoRollPlaying = false;
                btn_retry.IsEnabled = true;
                btn_continue.IsEnabled = true;
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
            double prog = (double)currentMeasureIndex / (double)allMeasure * 100;
            practiceDisplay.Text = $"Progress: {(int)prog}%";
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
            this.viewModel.PracticeLogic.CreatePractice(logic.PracticeNotes, this.viewModel.FileName, viewModel.PianorollLogic.TotalVisibleNotes, viewModel.PianorollLogic.MinOctave);
            Debug.WriteLine("Piano roll initialized for practice.");
            this.viewModel.PracticeLogic.LoadPractice();
            LoadPianoRoll();
        }

        private void UpdateFrame(object sender, EventArgs e)
        {
            if (isPianoRollInitialized && isPianoRollPlaying && !isWaiting && pianoRollCanvas != null)
            //if (isPianoRollInitialized && isPianoRollPlaying  && pianoRollCanvas != null)
            {
                UpdatePianoRollFrame();
            }

            if (!isMidiInAvailable)
            {
                for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                {
                    midiInDevices.Add(MidiIn.DeviceInfo(device).ProductName);
                }
            }

            if (!isMidiInAvailable && midiInDevices.Count > 0)
            {
                isMidiInAvailable = true;
                SetMidiIn();
            }

            if (isWaiting)
            {
                WaitingForMatch();
            }
        }

        private void WaitingForMatch()
        {
            bool egyezik = pushedPitches.Count == waitingPitches.Count &&
               pushedPitches.OrderBy(x => x).SequenceEqual(waitingPitches.OrderBy(x => x));
            if (egyezik && pushedPitches.Count != 0)
            {
                isWaiting = false;
                waitingPitches.Clear();
                pushedPitches.Clear();
            }
        }

        private void SetMidiIn()
        {
            midiInDevices.Add(MidiIn.DeviceInfo(0).ProductName);
            midiIn = new MidiIn(0);
            midiIn.MessageReceived += MidiIn_MessageReceived;

            midiConnectionDisplay.Text = $"Piano connected: {midiInDevices[0]}";
            midiConnectionDisplay.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#27AE60");

            btn_createPractice.IsEnabled = true;
            btn_loadPractice.IsEnabled = true;
            midiIn.Start();

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
                    if (!pushedPitches.Contains(midiNote))
                    {
                        pushedPitches.Add(midiNote);
                    }
                }
                // Ha a Velocity 0, az a billentyű Felengedését jelenti
                else
                {
                    pushedPitches.Remove(midiNote);
                }
            }
            // Kezeljük a dedikált NoteOff eseményt is (eszköze válogatja, melyiket küldi)
            else if (e.MidiEvent is NAudio.Midi.NoteEvent noteOff && e.MidiEvent.CommandCode == MidiCommandCode.NoteOff)
            {
                pushedPitches.Remove(noteOff.NoteNumber);
            }
        }

        private string ExtractValue(string text)
        {
            // A minta magyarázata:
            // Ch:\s+\d+\s+  -> Keresi a "Ch:" szót, utána szóközöket, számokat, majd megint szóközt
            // (?<ertek>.*?) -> Ez a "Capture Group": elmenti az összes karaktert egy 'ertek' nevű csoportba
            // \s+Vel        -> Egészen addig olvas, amíg szóközt és a "Vel" szót nem találja
            string pattern = @"Ch:\s+\d+\s+(?<ertek>.*?)\s+Vel";

            Match match = Regex.Match(text, pattern);

            if (match.Success)
            {
                return match.Groups["ertek"].Value.Trim();
            }

            return string.Empty; // Ha nem találja, üresen tér vissza
        }

        private int NAudioPitchToMidi(string pitch)
        {
            string step = pitch.Remove(pitch.Length - 1, 1);
            int octave = int.Parse(pitch.Substring(pitch.Length - 1, 1));
            int midiNote = NAudioNote[step] + (12 * octave);
            return midiNote;
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

        private void PracticeWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= UpdateFrame;
            //midiOut?.Dispose();
            isPianoRollInitialized = false;
            isPianoRollPlaying = false;
            pushedPitches.Clear();
            waitingPitches.Clear();

            // 1. Kötelező leiratkozás, különben memóriaszivárgás lesz!
            if (this.viewModel != null)
            {
                this.viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (this.midiIn != null)
            {
                try
                {
                    this.midiIn.Stop();
                    this.midiIn.Dispose();
                }
                catch { }
                this.isMidiInAvailable = false;
            }

            //this.viewModel.ToggleLogic.ToggleView();
            if (viewModel.PracticeLogic.Progress != null)
            {
                viewModel.PracticeLogic.SaveProgress(currentMeasureIndex);
            }
        }
    }
}
