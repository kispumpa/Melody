// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.UI
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.Windows.Threading;
    using Melody.Logic.Interfaces;
    using Melody.Logic.Models;
    using Melody.UI.ViewModels;
    using NAudio.Midi;

    /// <summary>Code behind for the MainWindow.xaml.</summary>
    public partial class MainWindow : UserControl
    {
        private const double PixelsPerSecond = 60;
        private const double PlaybackSpeed = 1.0;

        private Canvas pianoRollCanvas;
        private Canvas pianoKeysCanvas;
        private Dictionary<Note, Rectangle> noteRectangles;
        private bool isPianoRollInitialized = false;
        private bool isPianoRollPlaying = false;
        private bool isPaused = false;
        private DateTime pianoRollStartTime;
        private double canvasHeight;

        private bool isSheetMusicInitialized = false;
        private bool isSheetMusicPlaying = false;
        private DateTime sheetMusicStartTime;
        private DateTime pauseTiem;
        private double totalDuration = 0;
        private double totalSvgWidth = 0;

        private MainWindowViewModel viewModel;
        private MidiOut midiOut;

        private DispatcherTimer timer;
        private double speed = 4.5;

        /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
        public MainWindow()
        {
            //RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            this.InitializeComponent();

            this.noteRectangles = new Dictionary<Note, Rectangle>();

            this.DataContextChanged += this.MainWindow_DataContextChanged;

            this.Loaded += this.MainWindow_Loaded;
            this.Unloaded += this.MainWindow_Unloaded;

            this.timer = new DispatcherTimer();

            this.timer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainWindow loaded!");
            CompositionTarget.Rendering -= this.UpdateFrame;
            CompositionTarget.Rendering += this.UpdateFrame;

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
                this.midiOut?.Dispose();
                if (this.viewModel.SelectedMidiDeviceIndex >= 0)
                {
                    this.midiOut = new MidiOut(this.viewModel.SelectedMidiDeviceIndex);
                }
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.IsImageLoaded) && this.viewModel.IsImageLoaded)
            {
                Debug.WriteLine("Loading sheet music...");
                this.InitializeSheetMusic();
            }
        }

        private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MainWindowViewModel passedViewModel)
            {
                this.viewModel = passedViewModel;

                this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
            }
        }

        // ==================== PIANO ROLL IMPLEMENTATION ====================
        private void InitializePianoRoll()
        {
            if (this.isPianoRollInitialized)
            {
                this.pianorollGrid.Children.Clear();
                this.noteRectangles.Clear();
                this.isPianoRollInitialized = false;
            }

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
            if (this.canvasHeight <= 0)
            {
                this.canvasHeight = 800;
                Debug.WriteLine($"WARNING: Canvas height was 0, using {this.canvasHeight}");
            }

            logic.StoreNotes(this.ActualWidth);
            this.CreateNotesInCanvas(logic);

            this.isPianoRollPlaying = false;
            this.isPianoRollInitialized = true;
            this.CalculateTotalDuration();
            this.UpdateTimeDisplay(0);

            Debug.WriteLine($"Piano roll loaded! Canvas height: {this.canvasHeight}, Notes: {logic.LoadedNotes.Count}");
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

                Rectangle whiteKey = new Rectangle
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
                    Rectangle blackKey = new Rectangle
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
                    TextBlock label = new TextBlock
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

            foreach (Note note in logic.LoadedNotes)
            {
                Rectangle rect = new Rectangle
                {
                    Width = note.X.Length,
                    Height = note.Y.Length,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
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

        // ==================== SHEET MUSIC IMPLEMENTATION ====================
        private void InitializeSheetMusic()
        {
            try
            {
                if (this.viewModel.ImagePaths == null || this.viewModel.ImagePaths.Count == 0)
                {
                    Debug.WriteLine("No PNG files loaded!");
                    return;
                }

                this.myPlaybackCursor.Y2 = this.sheetMusicGrid.ActualHeight;
                this.myPlaybackCursor.Visibility = Visibility.Visible;

                //ImageControl.Source = new BitmapImage(new Uri(viewModel.ImagePaths[0]));

                //BitmapImage bitmap = new BitmapImage();
                //bitmap.BeginInit();
                //bitmap.UriSource = new Uri(viewModel.ImagePaths[0], UriKind.Absolute);
                //bitmap.CacheOption = BitmapCacheOption.OnLoad;
                //bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // <-- EZ A KULCS!
                //bitmap.EndInit();

                //ImageControl.Source = bitmap;
                BitmapImage bitmap = new BitmapImage();

                // A FileShare.ReadWrite biztosítja, hogy ne fagyjon ki, ha valami még fogná a fájlt
                using (System.IO.FileStream stream = new System.IO.FileStream(this.viewModel.ImagePaths[0], System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }

                bitmap.Freeze(); // Ez nagyon fontos a WPF-ben! Gyorsítja a renderelést és leválasztja a szálról.

                this.ImageControl.Source = null;   // Biztos, ami biztos: töröljük a régi képet a felületről
                this.ImageControl.Source = bitmap; // Rárakjuk a vadonatújat

                this.CalculateTotalDuration();
                this.ImageTransform.X = 300;
                this.isSheetMusicInitialized = true;

                Debug.WriteLine($"Sheet music initialized! {this.viewModel.ImagePaths.Count} PNG(s) loaded");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sheet music: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== PLAYBACK CONTROLS ====================
        private void CalculateTotalDuration()
        {
            IPianorollLogic logic = this.viewModel.PianorollLogic;

            if (logic?.LoadedNotes != null && logic.LoadedNotes.Count > 0)
            {
                this.totalDuration = logic.LoadedNotes.Max(n => n.Y.Position + n.Y.Length) / PixelsPerSecond;
            }
            else
            {
                this.totalDuration = 60;
            }

            this.UpdateTimeDisplay(0);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.isSheetMusicInitialized && !this.isPianoRollInitialized)
            {
                MessageBox.Show("Please load a file first!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            this.playButton.IsEnabled = false;
            this.pauseButton.IsEnabled = true;
            this.stopButton.IsEnabled = true;

            if (this.isPaused)
            {
                this.pianoRollStartTime = this.pianoRollStartTime.Add(DateTime.Now - this.pauseTiem);
                this.isPaused = false;
            }
            else
            {
                this.pianoRollStartTime = DateTime.Now;
                this.sheetMusicStartTime = DateTime.Now;
            }

            this.isPianoRollPlaying = true;
            this.isSheetMusicPlaying = true;

            this.myPlaybackCursor.Visibility = Visibility.Visible;

            // timer.Start();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            this.isSheetMusicPlaying = false;
            this.isPianoRollPlaying = false;

            this.playButton.IsEnabled = true;
            this.pauseButton.IsEnabled = false;
            this.stopButton.IsEnabled = true;

            this.isPaused = true;
            this.pauseTiem = DateTime.Now;

            Debug.WriteLine("Playback paused");
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            this.isSheetMusicPlaying = false;
            this.isPianoRollPlaying = false;

            this.playButton.IsEnabled = true;
            this.pauseButton.IsEnabled = false;
            this.stopButton.IsEnabled = false;

            this.UpdateTimeDisplay(0);

            IPianorollLogic logic = this.viewModel.PianorollLogic;
            if (logic?.LoadedNotes != null)
            {
                foreach (Note note in logic.LoadedNotes)
                {
                    note.Played = false;
                }
            }

            if (this.isPianoRollInitialized)
            {
                this.pianoRollStartTime = DateTime.Now;
                this.UpdatePianoRollFrame();
            }

            this.ImageTransform.X = 300;

            Debug.WriteLine("Playback stopped");
        }

        // ==================== UPDATE LOOP ====================
        private void UpdateFrame(object sender, EventArgs e)
        {
            if (this.isPianoRollInitialized && this.isPianoRollPlaying && this.pianoRollCanvas != null && this.isSheetMusicInitialized && this.isSheetMusicPlaying)
            {
                this.UpdatePianoRollFrame();
                this.UpdateSheetMusicFrame();
                this.UpdateTimeDisplay((DateTime.Now - this.pianoRollStartTime).TotalSeconds * PlaybackSpeed);
            }
        }

        private void UpdatePianoRollFrame()
        {
            double elapsed = (DateTime.Now - this.pianoRollStartTime).TotalSeconds * PlaybackSpeed;

            foreach (KeyValuePair<Note, Rectangle> kvp in this.noteRectangles)
            {
                Note note = kvp.Key;
                Rectangle rect = kvp.Value;

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
                    note.Played = true;
                }
            }

            if (elapsed >= this.totalDuration)
            {
                this.StopButton_Click(null, null);
            }
        }

        private void UpdateSheetMusicFrame()
        {
            double elapsed = (DateTime.Now - this.pianoRollStartTime).TotalSeconds * PlaybackSpeed;

            // A sebességed (pixel / másodperc). A 4.5/képkocka nagyjából 270 pixel/másodpercnek felel meg.
            // Ezt a számot kell növelned/csökkentened a tökéletes sebességhez!


            // Az új X pozíció: Kezdőpont (300) mínusz (eltelt idő * sebesség)
            this.ImageTransform.X = 300 - (elapsed * (PixelsPerSecond + 4));
        }

        private void UpdateTimeDisplay(double currentTime)
        {
            TimeSpan current = TimeSpan.FromSeconds(currentTime);
            TimeSpan total = TimeSpan.FromSeconds(this.totalDuration);

            this.timeDisplay.Text = $"{current:mm\\:ss} / {total:mm\\:ss}";
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
                int midiNote = this.PitchToMidi(pitch);
                this.midiOut.Send(MidiMessage.StartNote(midiNote, 60, 1).RawData);

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
            int midiNote = (int)(MusicNote)Enum.Parse(typeof(MusicNote), step) + (12 * octave);
            return midiNote;
        }

        // ==================== CLEANUP ====================
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            this.StopButton_Click(null, null);
            CompositionTarget.Rendering -= this.UpdateFrame;
            this.midiOut?.Dispose();
            this.isPianoRollPlaying = false;
            this.isSheetMusicPlaying = false;
            if (this.viewModel != null)
            {
                this.viewModel.PropertyChanged -= this.ViewModel_PropertyChanged;
            }
        }
    }
}