using System;
using System.Windows;
using SmartMepRouter.UI.ViewModels;

namespace SmartMepRouter.UI.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainViewModel ViewModel { get; }

        public event EventHandler? PickStartRequested;
        public event EventHandler? PickEndRequested;
        public event EventHandler? PreviewRequested;
        public event EventHandler? GenerateRequested;

        public MainView(MainViewModel? viewModel = null)
        {
            InitializeComponent();
            ViewModel = viewModel ?? new MainViewModel();
            DataContext = ViewModel;
        }

        private void PickStart_Click(object sender, RoutedEventArgs e)
        {
            PickStartRequested?.Invoke(this, EventArgs.Empty);
        }

        private void PickEnd_Click(object sender, RoutedEventArgs e)
        {
            PickEndRequested?.Invoke(this, EventArgs.Empty);
        }

        private void PreviewPath_Click(object sender, RoutedEventArgs e)
        {
            PreviewRequested?.Invoke(this, EventArgs.Empty);
        }

        private void GenerateDucts_Click(object sender, RoutedEventArgs e)
        {
            GenerateRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
