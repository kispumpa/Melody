using CommunityToolkit.Mvvm.DependencyInjection;
using Melody.Logic.Interfaces;
using Melody.Logic.Models;
using Melody.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Melody.UI
{
    /// <summary>
    /// Interaction logic for PracticeWindow.xaml
    /// </summary>
    public partial class PracticeWindow : Window
    {
        private PracticeWindowViewModel viewModel;
        public PracticeWindow()
        {
            InitializeComponent();
            this.viewModel = new ViewModels.PracticeWindowViewModel(Ioc.Default.GetService<IPianorollLogic>(),
              Ioc.Default.GetService<IMxlUnpacker>(),
              Ioc.Default.GetService<IPracticeLogic>(),
              Ioc.Default.GetService<IToggleViewLogic>());

            this.DataContext = this.viewModel;
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
              
            Debug.WriteLine("Piano roll loaded for practice.");
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
