using CommunityToolkit.Mvvm.DependencyInjection;
using Melody.Logic;
using Melody.Logic.Interfaces;
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
              Ioc.Default.GetService<IPracticeLogic>());

            this.DataContext = this.viewModel;
            this.viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PracticeWindowViewModel.IsPianorollLoaded))
            {
                Debug.WriteLine("Loading piano rol...");
                this.InitializePianoRoll();
            }
        }

        private void InitializePianoRoll()
        {
            var logic = this.viewModel.PianorollLogic;
            logic.StoreNotes(this.ActualWidth, isPractice: true);
            this.viewModel.PracticeLogic.CreatePractice(logic.PracticeNotes, this.viewModel.FileName);

        }
    }
}
