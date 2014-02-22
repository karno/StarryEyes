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
            var viewModel = this.DataContext as SlimNotificatorViewModel;
            this.Left = viewModel.Left;
            this.Top = viewModel.Top;
            this.Width = viewModel.Width;
        }
    }
}
