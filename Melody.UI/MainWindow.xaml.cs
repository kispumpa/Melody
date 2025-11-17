using CommunityToolkit.Mvvm.DependencyInjection;
using Melody.Logic.Interfaces;
using Melody.Logic.Models;
using NAudio.Midi;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Melody.UI
{
    public partial class MainWindow : Window
    {
        // ===== KONSTANSOK =====
        private const double PixelsPerSecond = 60;
        private const double PlaybackSpeed = 1.0;

        // ===== MEZŐK =====
        private Canvas pianoRollCanvas;
        private Canvas pianoKeysCanvas;
        private MainWindowViewModel viewModel;
        private Dictionary<Note, Rectangle> noteRectangles;

        private bool isInitialized = false;
        private bool isPlaying = false;
        private DateTime startTime;
        private double canvasHeight;
        private MidiOut midiOut;

        public MainWindow()
        {
            InitializeComponent();

            noteRectangles = new Dictionary<Note, Rectangle>();

            viewModel = new MainWindowViewModel(
                Ioc.Default.GetService<IToggleViewLogic>(),
                Ioc.Default.GetService<ILilypondLogic>(),
                Ioc.Default.GetService<IPianorollLogic>()
            );

            this.DataContext = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // ✅ 60 FPS rendering loop
            CompositionTarget.Rendering += UpdateFrame;

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainWindow loaded!");

            // MIDI eszköz inicializálás
            if (viewModel.SelectedMidiDeviceIndex >= 0)
            {
                midiOut = new MidiOut(viewModel.SelectedMidiDeviceIndex);
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsPianorollLoaded) && viewModel.IsPianorollLoaded)
            {
                Debug.WriteLine("Initializing piano roll...");
                InitializePianoRoll();
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.SelectedMidiDeviceIndex))
            {
                // MIDI eszköz váltás
                midiOut?.Dispose();
                if (viewModel.SelectedMidiDeviceIndex >= 0)
                {
                    midiOut = new MidiOut(viewModel.SelectedMidiDeviceIndex);
                }
            }
        }

        // ===== PIANO ROLL INICIALIZÁLÁS =====
        private void InitializePianoRoll()
        {
            if (isInitialized)
            {
                // Ha újra betöltünk, tisztítsuk meg
                pianorollGrid.Children.Clear();
                noteRectangles.Clear();
                isInitialized = false;
            }

            var logic = viewModel.PianorollLogic;

            // Piano billentyűk létrehozása
            pianoKeysCanvas = CreatePianoKeys(logic);
            pianorollGrid.Children.Add(pianoKeysCanvas);
            Grid.SetRow(pianoKeysCanvas, 1);

            // Piano roll canvas létrehozása
            pianoRollCanvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(200, 230, 255)),
                ClipToBounds = true
            };
            pianorollGrid.Children.Add(pianoRollCanvas);
            Grid.SetRow(pianoRollCanvas, 0);

            // Canvas magasság tárolása
            this.UpdateLayout(); // ✅ Fontos: layout frissítés
            canvasHeight = pianorollGrid.RowDefinitions[0].ActualHeight;
            if (canvasHeight <= 0)
            {
                canvasHeight = 800; // Teszt érték
                Debug.WriteLine($"WARNING: Canvas height was 0, using {canvasHeight}");
            }

            // Hangjegyek számítása
            logic.StoreNotes(this.ActualWidth);

            // Rectangle-ek létrehozása
            CreateNotesInCanvas(logic);

            // Lejátszás indítása
            startTime = DateTime.Now;
            isPlaying = true;

            isInitialized = true;

            Debug.WriteLine($"Piano roll initialized! Canvas height: {canvasHeight}, Notes: {logic.LoadedNotes.Count}");
        }

        // ===== PIANO BILLENTYŰK =====
        private Canvas CreatePianoKeys(IPianorollLogic logic)
        {
            var canvas = new Canvas
            {
                Width = this.ActualWidth,
                Height = pianorollGrid.RowDefinitions[1].ActualHeight
            };

            double keyWidth = this.ActualWidth / logic.TotalVisibleNotes;
            double keyHeight = pianorollGrid.RowDefinitions[1].ActualHeight;

            for (int i = 0; i < logic.TotalVisibleNotes; i++)
            {
                int noteValue = i % 7;
                bool hasBlackKey = (noteValue == 0 || noteValue == 1 || noteValue == 3 || noteValue == 4 || noteValue == 5);

                // Fehér billentyű
                var whiteKey = new Rectangle
                {
                    Width = keyWidth,
                    Height = keyHeight,
                    Fill = Brushes.White,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };
                canvas.Children.Add(whiteKey);
                Canvas.SetLeft(whiteKey, keyWidth * i);

                // Fekete billentyű
                if (hasBlackKey)
                {
                    var blackKey = new Rectangle
                    {
                        Width = keyWidth / 2,
                        Height = keyHeight / 2,
                        Fill = Brushes.Black,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 1
                    };
                    canvas.Children.Add(blackKey);
                    Canvas.SetLeft(blackKey, keyWidth * (i + 0.5));
                    Canvas.SetTop(blackKey, 0);
                }

                // C hangoknál címke
                if (noteValue == 0)
                {
                    var label = new TextBlock
                    {
                        Text = $"{(Step)noteValue}{logic.MinOctave + i / 7}",
                        FontSize = 11,
                        Foreground = Brushes.Black
                    };
                    canvas.Children.Add(label);
                    Canvas.SetLeft(label, keyWidth * i + 2);
                    Canvas.SetBottom(label, 2);
                }
            }

            return canvas;
        }

        // ===== HANGJEGYEK LÉTREHOZÁSA =====
        private void CreateNotesInCanvas(IPianorollLogic logic)
        {
            noteRectangles.Clear();

            foreach (var note in logic.LoadedNotes)
            {
                var rect = new Rectangle
                {
                    Width = note.X.Length,
                    Height = note.Y.Length,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 165, 0)), // Narancs
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Visibility = Visibility.Hidden // ✅ Kezdetben rejtett
                };

                Canvas.SetLeft(rect, note.X.Position);
                pianoRollCanvas.Children.Add(rect);
                noteRectangles[note] = rect;
            }

            Debug.WriteLine($"Created {noteRectangles.Count} note rectangles");
        }

        // ===== FRAME FRISSÍTÉS (60 FPS) =====
        private void UpdateFrame(object sender, EventArgs e)
        {
            if (!isInitialized || !isPlaying || pianoRollCanvas == null)
                return;

            double elapsed = (DateTime.Now - startTime).TotalSeconds * PlaybackSpeed;

            foreach (var kvp in noteRectangles)
            {
                var note = kvp.Key;
                var rect = kvp.Value;

                double y = note.Y.Position - (elapsed * PixelsPerSecond);

                // Láthatóság ellenőrzése
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

                // Hang lejátszása (amikor eléri az aljat)
                if (!note.Played && y <= 0 && y > -note.Y.Length)
                {
                    PlayNote(note.Pitch, (int)note.Y.Length);
                    note.Played = true;
                }
            }
        }

        // ===== MIDI LEJÁTSZÁS =====
        private void PlayNote(string pitch, int durationPixels)
        {
            if (midiOut == null) return;

            try
            {
                int midiNote = PitchToMidi(pitch);

                midiOut.Send(MidiMessage.StartNote(midiNote, 60, 1).RawData);

                int durationMs = (int)((durationPixels / PixelsPerSecond) * 1000);

                Debug.WriteLine($"Playing note {pitch} (MIDI {midiNote}) for {durationMs}ms");

                Task.Delay(durationMs).ContinueWith(_ =>
                {
                    midiOut?.Send(MidiMessage.StopNote(midiNote, 60, 1).RawData);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MIDI error: {ex.Message}");
            }
        }

        private int PitchToMidi(string pitch)
        {
            string step = pitch.Remove(pitch.Length - 1, 1);
            int octave = int.Parse(pitch.Substring(pitch.Length - 1, 1));
            var midiNote = (int)(MusicNote)Enum.Parse(typeof(MusicNote), step) + 12 * octave;
            return midiNote;
        }

        // ===== CLEANUP =====
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= UpdateFrame;
            midiOut?.Dispose();
            isPlaying = false;
        }
    }
}