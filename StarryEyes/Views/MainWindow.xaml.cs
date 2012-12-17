using MahApps.Metro.Controls;
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
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var viewModel = this.DataContext as MainWindowViewModel;
            if (viewModel != null && !viewModel.OnClosing())
            {
                e.Cancel = true;
            }
        }
    }
}
