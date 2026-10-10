using System;
using System.ComponentModel;
using System.Windows;
using SmartMepRouter.UI.Settings;
using SmartMepRouter.UI.ViewModels;

namespace SmartMepRouter.UI.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private readonly SettingsStore _settingsStore = new SettingsStore();

        public MainViewModel ViewModel { get; }

        public event EventHandler? PickStartRequested;
        public event EventHandler? PickEndRequested;
        /// <summary>Raised by the Preview button; while the view model is busy it means "cancel".</summary>
        public event EventHandler? PreviewRequested;
        public event EventHandler? GenerateRequested;

        public MainView(MainViewModel? viewModel = null)
        {
            InitializeComponent();
            ViewModel = viewModel ?? new MainViewModel();
            ViewModel.ApplySettings(_settingsStore.Load());
            DataContext = ViewModel;
            ApplyTheme();

            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.StatusMessage)) LogScroll.ScrollToEnd();
                if (e.PropertyName == nameof(MainViewModel.IsDarkTheme)) ApplyTheme();
            };
        }

        private void ApplyTheme()
        {
            string file = ViewModel.IsDarkTheme ? "Tokens.Dark.xaml" : "Tokens.Light.xaml";
            var tokens = new ResourceDictionary
            {
                Source = new Uri($"/SmartMepRouter.UI;component/Themes/{file}", UriKind.Relative)
            };

            // The first merged dictionary of Window.Resources is the token set (see MainView.xaml).
            if (Resources.MergedDictionaries.Count > 0)
                Resources.MergedDictionaries[0] = tokens;
            else
                Resources.MergedDictionaries.Add(tokens);
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsDarkTheme = !ViewModel.IsDarkTheme;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _settingsStore.Save(ViewModel.ToSettings());
            base.OnClosing(e);
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
