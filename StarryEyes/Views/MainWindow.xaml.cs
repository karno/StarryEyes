using MahApps.Metro.Controls;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.ViewModels;

namespace StarryEyes.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!Setting.IsLoaded) return;
            var rect = Setting.LastWindowPosition.Value;
            var state = Setting.LastWindowState.Value;
            if (rect.IsEmpty) return;
            this.SetWindowPlacement(rect);
            this.WindowState = state;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var viewModel = this.DataContext as MainWindowViewModel;
            if (viewModel != null && !viewModel.OnClosing())
            {
                e.Cancel = true;
            }
            else
            {
                Setting.LastWindowPosition.Value = this.GetWindowPlacement();
                Setting.LastWindowState.Value = this.WindowState;
            }
        }
    }
}
