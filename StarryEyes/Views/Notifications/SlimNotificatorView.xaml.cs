using System.Windows;
using StarryEyes.ViewModels.Notifications;

namespace StarryEyes.Views.Notifications
{
    /// <summary>
    /// SlimNotificatorView.xaml の相互作用ロジック
    /// </summary>
    public partial class SlimNotificatorView : Window
    {
        public SlimNotificatorView()
        {
            InitializeComponent();
        }

        private void SlimNotificatorView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SlimNotificatorViewModel;
            if (viewModel == null) return;
            Left = viewModel.Left;
            Top = viewModel.Top;
            Width = viewModel.Width;
        }
    }
}
