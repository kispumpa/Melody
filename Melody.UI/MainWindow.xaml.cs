using CommunityToolkit.Mvvm.DependencyInjection;
using Melody.Logic.Interfaces;
using Melody.Logic.Models;
using NAudio.Midi;
using SharpVectors.Converters;
using System.Diagnostics;
using System.IO;
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

        // ===== PIANO ROLL MEZŐK =====
        private Canvas pianoRollCanvas;
        private Canvas pianoKeysCanvas;
        private Dictionary<Note, Rectangle> noteRectangles;
        private bool isPianoRollInitialized = false;
        private bool isPianoRollPlaying = false;
        private DateTime pianoRollStartTime;
        private double canvasHeight;

        // ===== SHEET MUSIC MEZŐK =====
        private bool isSheetMusicInitialized = false;
        private bool isSheetMusicPlaying = false;
        private DateTime sheetMusicStartTime;
        private Line playbackCursor;
        private double totalDuration = 0;

        // ===== KÖZÖS MEZŐK =====
        private MainWindowViewModel viewModel;
        private MidiOut midiOut;

        public MainWindow()
        {
            this.InitializeComponent();

            this.noteRectangles = new Dictionary<Note, Rectangle>();

            this.viewModel = new MainWindowViewModel(
                Ioc.Default.GetService<IToggleViewLogic>(),
                Ioc.Default.GetService<ILilypondLogic>(),
                Ioc.Default.GetService<IPianorollLogic>(),
                Ioc.Default.GetService<IMxlUnpacker>());

            this.DataContext = this.viewModel;
            this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;

            // 60 FPS rendering loop
            CompositionTarget.Rendering += UpdateFrame;

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
                Debug.WriteLine("Loading piano roll...");
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
            else if (e.PropertyName == nameof(MainWindowViewModel.IsSvgLoaded) && viewModel.IsSvgLoaded)
            {
                Debug.WriteLine("Loading sheet music...");
                InitializeSheetMusic();
            }
        }

        // ==================== PIANO ROLL IMPLEMENTATION ====================

        private void InitializePianoRoll()
        {
            if (isPianoRollInitialized)
            {
                pianorollGrid.Children.Clear();
                noteRectangles.Clear();
                isPianoRollInitialized = false;
            }

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
            if (canvasHeight <= 0)
            {
                canvasHeight = 800;
                Debug.WriteLine($"WARNING: Canvas height was 0, using {canvasHeight}");
            }

            logic.StoreNotes(this.ActualWidth);
            CreateNotesInCanvas(logic);

            //pianoRollStartTime = DateTime.Now;
            isPianoRollPlaying = false;
            isPianoRollInitialized = true;
            CalculateTotalDuration(); // Ez fontos, hogy tudjuk a hosszát
            UpdateTimeDisplay(0);

            Debug.WriteLine($"Piano roll loaded! Canvas height: {this.canvasHeight}, Notes: {logic.LoadedNotes.Count}");
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

        private void CreateNotesInCanvas(IPianorollLogic logic)
        {
            this.noteRectangles.Clear();

            foreach (var note in logic.LoadedNotes)
            {
                var rect = new Rectangle
                {
                    Width = note.X.Length,
                    Height = note.Y.Length,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Visibility = Visibility.Hidden
                };

                Canvas.SetLeft(rect, note.X.Position);
                this.pianoRollCanvas.Children.Add(rect);
                this.noteRectangles[note] = rect;
            }

            Debug.WriteLine($"Created {this.noteRectangles.Count} note rectangles");
        }

        // ==================== SHEET MUSIC IMPLEMENTATION ====================

        private void InitializeSheetMusic()
        {
            try
            {
                string svgPath = viewModel.SvgSource;

                if (string.IsNullOrEmpty(svgPath) || !File.Exists(svgPath))
                {
                    Debug.WriteLine("SVG file not found!");
                    return;
                }

                // SVG betöltése
                svgViewbox.Source = new Uri(System.IO.Path.GetFullPath(svgPath));

                this.UpdateLayout();

                // Playback kurzor létrehozása
                myPlaybackCursor.Y2 = this.sheetMusicGrid.ActualHeight;

                // Alaphelyzetbe állítjuk a transzformációt (késleltetés, hogy a Viewbox betöltsön)
                scoreTransform.X = myPlaybackCursor.X1; // A kotta eleje a kurzornál kezdődik

                CalculateTotalDuration();
                isSheetMusicInitialized = true;

                Debug.WriteLine($"Sheet music initialized! SVG loaded from: {svgPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sheet music: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Sheet music error: {ex}");
            }
        }

        private void CalculateTotalDuration()
        {
            var logic = viewModel.PianorollLogic;

            if (logic?.LoadedNotes != null && logic.LoadedNotes.Count > 0)
            {
                // A legutolsó hang végének időpontja
                totalDuration = logic.LoadedNotes.Max(n => n.Y.Position + n.Y.Length) / PixelsPerSecond;
            }
            else
            {
                totalDuration = 60; // Alapértelmezett 60 másodperc
            }

            UpdateTimeDisplay(0);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Ellenőrizzük, hogy van-e betöltve valami
            if (!isSheetMusicInitialized && !isPianoRollInitialized)
            {
                MessageBox.Show("Please load a file first!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Gombok állapotának frissítése
            playButton.IsEnabled = false;
            pauseButton.IsEnabled = true;
            stopButton.IsEnabled = true;

            // Logika elágaztatása a nézet alapján
            if (viewModel.IsPianoRollView)
            {
                // PIANO ROLL INDÍTÁSA
                if (isPianoRollInitialized)
                {
                    // Újraindítás (egyszerűsített logika, hasonlóan a kottához)
                    pianoRollStartTime = DateTime.Now;

                    // Hangjegyek állapotának alaphelyzetbe állítása
                    foreach (var note in viewModel.PianorollLogic.LoadedNotes)
                    {
                        note.Played = false;
                    }

                    isPianoRollPlaying = true;
                }
            }
            else
            {
                // SHEET MUSIC INDÍTÁSA (Meglévő kód)
                sheetMusicStartTime = DateTime.Now;
                isSheetMusicPlaying = true;
                myPlaybackCursor.Visibility = Visibility.Visible;

                // ... (meglévő Sheet Music logika) ...
                var logic = viewModel.PianorollLogic;
                if (logic?.LoadedNotes != null)
                {
                    logic.StoreNotes(this.ActualWidth);
                    foreach (var note in logic.LoadedNotes) { note.Played = false; }
                }
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            isSheetMusicPlaying = false;
            playButton.IsEnabled = true;
            isPianoRollPlaying = false;
            pauseButton.IsEnabled = false;

            Debug.WriteLine("Playback paused");
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            isSheetMusicPlaying = false;
            isPianoRollPlaying = false;

            playButton.IsEnabled = true;
            pauseButton.IsEnabled = false;
            stopButton.IsEnabled = false;

            UpdateTimeDisplay(0);

            // Reset played notes
            var logic = viewModel.PianorollLogic;
            if (logic?.LoadedNotes != null)
            {
                foreach (var note in logic.LoadedNotes)
                {
                    note.Played = false;
                }
            }
            if (isPianoRollInitialized)
            {
                // Frissítsük egyszer a frame-et 0 eltelt idővel, hogy visszaálljon a grafika
                // Ehhez egy trükk: ideiglenesen beállítjuk a StartTime-ot "mostra", 
                // meghívjuk a frissítést, majd kikapcsoljuk a playing flaget.
                pianoRollStartTime = DateTime.Now;
                UpdatePianoRollFrame();
            }

            Debug.WriteLine("Playback stopped");
        }

        // ==================== UPDATE LOOP ====================

        private void UpdateFrame(object sender, EventArgs e)
        {
            // Piano Roll frissítése
            if (isPianoRollInitialized && isPianoRollPlaying && pianoRollCanvas != null)
            {
                UpdatePianoRollFrame();
            }

            // Sheet Music frissítése
            if (isSheetMusicInitialized && isSheetMusicPlaying)
            {
                UpdateSheetMusicFrame();
            }
        }

        private void UpdatePianoRollFrame()
        {
            double elapsed = (DateTime.Now - pianoRollStartTime).TotalSeconds * PlaybackSpeed;
            UpdateTimeDisplay(elapsed);
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

                if (!note.Played && y <= 0 && y > -note.Y.Length)
                {
                    this.PlayNote(note.Pitch, (int)note.Y.Length);
                    note.Played = true;
                }
            }
            if (elapsed >= totalDuration)
            {
                StopButton_Click(null, null);
            }
        }

        private void UpdateSheetMusicFrame()
        {
            double elapsed = (DateTime.Now - sheetMusicStartTime).TotalSeconds * PlaybackSpeed;

            // Kurzor mozgatása
            double totalWidth = svgViewbox.ActualWidth;

            // 2. Kiszámoljuk, hol tartunk pixelben
            // (Arányosítás: eltelt idő / teljes idő * teljes szélesség)
            double currentPixelPos = (elapsed / totalDuration) * totalWidth;

            // 3. Mozgatás:
            // A kurzor fix pozíciója (pl. 100px) MÍNUSZ a jelenlegi pozíció.
            // Így a kotta "befolyik" a kurzor alá.
            double cursorFixedPosition = myPlaybackCursor.X1;
            scoreTransform.X = cursorFixedPosition - currentPixelPos;

            // Hangjegyek lejátszása
            var logic = viewModel.PianorollLogic;
            if (logic?.LoadedNotes != null)
            {
                foreach (var note in logic.LoadedNotes)
                {
                    double noteTime = note.Y.Position / PixelsPerSecond;

                    if (!note.Played && elapsed >= noteTime)
                    {
                        PlayNote(note.Pitch, (int)note.Y.Length);
                        note.Played = true;
                    }
                }
            }

            // Idő kijelzés frissítése
            UpdateTimeDisplay(elapsed);

            // Lejátszás vége
            if (elapsed >= totalDuration)
            {
                StopButton_Click(null, null);
            }
        }

        private void UpdateTimeDisplay(double currentTime)
        {
            TimeSpan current = TimeSpan.FromSeconds(currentTime);
            TimeSpan total = TimeSpan.FromSeconds(totalDuration);

            timeDisplay.Text = $"{current:mm\\:ss} / {total:mm\\:ss}";
        }

        // ==================== MIDI PLAYBACK ====================

        private void PlayNote(string pitch, int durationPixels)
        {
            if (this.midiOut == null)
            {
                return;
            }

            try
            {
                int midiNote = PitchToMidi(pitch);
                midiOut.Send(MidiMessage.StartNote(midiNote, 60, 1).RawData);

                int durationMs = (int)((durationPixels / PixelsPerSecond) * 1000);

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

        // ==================== CLEANUP ====================

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= UpdateFrame;
            midiOut?.Dispose();
            isPianoRollPlaying = false;
            isSheetMusicPlaying = false;
        }
    }
}