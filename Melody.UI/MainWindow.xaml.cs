// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using Melody.Logic.Interfaces;
    using Melody.Logic.Models;
    using NAudio.Midi;

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
            this.InitializeComponent();

            this.noteRectangles = new Dictionary<Note, Rectangle>();

            this.viewModel = new MainWindowViewModel(
                Ioc.Default.GetService<IToggleViewLogic>(),
                Ioc.Default.GetService<ILilypondLogic>(),
                Ioc.Default.GetService<IPianorollLogic>());

            this.DataContext = this.viewModel;
            this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;

            // ✅ 60 FPS rendering loop
            CompositionTarget.Rendering += this.UpdateFrame;

            this.Loaded += this.MainWindow_Loaded;
            this.Closing += this.MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainWindow loaded!");

            // MIDI eszköz inicializálás
            if (this.viewModel.SelectedMidiDeviceIndex >= 0)
            {
                this.midiOut = new MidiOut(this.viewModel.SelectedMidiDeviceIndex);
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsPianorollLoaded) && this.viewModel.IsPianorollLoaded)
            {
                Debug.WriteLine("Initializing piano roll...");
                this.InitializePianoRoll();
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.SelectedMidiDeviceIndex))
            {
                // MIDI eszköz váltás
                this.midiOut?.Dispose();
                if (this.viewModel.SelectedMidiDeviceIndex >= 0)
                {
                    this.midiOut = new MidiOut(this.viewModel.SelectedMidiDeviceIndex);
                }
            }
        }

        // ===== PIANO ROLL INICIALIZÁLÁS =====
        private void InitializePianoRoll()
        {
            if (this.isInitialized)
            {
                // Ha újra betöltünk, tisztítsuk meg
                this.pianorollGrid.Children.Clear();
                this.noteRectangles.Clear();
                this.isInitialized = false;
            }

            var logic = this.viewModel.PianorollLogic;

            // Piano billentyűk létrehozása
            this.pianoKeysCanvas = this.CreatePianoKeys(logic);
            this.pianorollGrid.Children.Add(this.pianoKeysCanvas);
            Grid.SetRow(this.pianoKeysCanvas, 1);

            // Piano roll canvas létrehozása
            this.pianoRollCanvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(200, 230, 255)),
                ClipToBounds = true,
            };
            this.pianorollGrid.Children.Add(this.pianoRollCanvas);
            Grid.SetRow(this.pianoRollCanvas, 0);

            // Canvas magasság tárolása
            this.UpdateLayout(); // ✅ Fontos: layout frissítés
            this.canvasHeight = this.pianorollGrid.RowDefinitions[0].ActualHeight;
            if (this.canvasHeight <= 0)
            {
                this.canvasHeight = 800; // Teszt érték
                Debug.WriteLine($"WARNING: Canvas height was 0, using {this.canvasHeight}");
            }

            // Hangjegyek számítása
            logic.StoreNotes(this.ActualWidth);

            // Rectangle-ek létrehozása
            this.CreateNotesInCanvas(logic);

            // Lejátszás indítása
            this.startTime = DateTime.Now;
            this.isPlaying = true;

            this.isInitialized = true;

            Debug.WriteLine($"Piano roll initialized! Canvas height: {this.canvasHeight}, Notes: {logic.LoadedNotes.Count}");
        }

        // ===== PIANO BILLENTYŰK =====
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

                // Fehér billentyű
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

                // Fekete billentyű
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

                // C hangoknál címke
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

        // ===== HANGJEGYEK LÉTREHOZÁSA =====
        private void CreateNotesInCanvas(IPianorollLogic logic)
        {
            this.noteRectangles.Clear();

            foreach (var note in logic.LoadedNotes)
            {
                var rect = new Rectangle
                {
                    Width = note.X.Length,
                    Height = note.Y.Length,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 165, 0)), // Narancs
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Visibility = Visibility.Hidden,
                };

                Canvas.SetLeft(rect, note.X.Position);
                this.pianoRollCanvas.Children.Add(rect);
                this.noteRectangles[note] = rect;
            }

            Debug.WriteLine($"Created {this.noteRectangles.Count} note rectangles");
        }

        // ===== FRAME FRISSÍTÉS (60 FPS) =====
        private void UpdateFrame(object sender, EventArgs e)
        {
            if (!this.isInitialized || !this.isPlaying || this.pianoRollCanvas == null)
            {
                return;
            }

            double elapsed = (DateTime.Now - this.startTime).TotalSeconds * PlaybackSpeed;

            foreach (var kvp in this.noteRectangles)
            {
                var note = kvp.Key;
                var rect = kvp.Value;

                double y = note.Y.Position - (elapsed * PixelsPerSecond);

                // Láthatóság ellenőrzése
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

                // Hang lejátszása (amikor eléri az aljat)
                if (!note.Played && y <= 0 && y > -note.Y.Length)
                {
                    this.PlayNote(note.Pitch, (int)note.Y.Length);
                    note.Played = true;
                }
            }
        }

        // ===== MIDI LEJÁTSZÁS =====
        private void PlayNote(string pitch, int durationPixels)
        {
            if (this.midiOut == null)
            {
                return;
            }

            try
            {
                int midiNote = this.PitchToMidi(pitch);

                this.midiOut.Send(MidiMessage.StartNote(midiNote, 60, 1).RawData);

                int durationMs = (int)((durationPixels / PixelsPerSecond) * 1000);

                Debug.WriteLine($"Playing note {pitch} (MIDI {midiNote}) for {durationMs}ms");

                Task.Delay(durationMs).ContinueWith(_ =>
                {
                    this.midiOut?.Send(MidiMessage.StopNote(midiNote, 60, 1).RawData);
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
            var midiNote = (int)(MusicNote)Enum.Parse(typeof(MusicNote), step) + (12 * octave);
            return midiNote;
        }

        // ===== CLEANUP =====
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= this.UpdateFrame;
            this.midiOut?.Dispose();
            this.isPlaying = false;
        }
    }
}