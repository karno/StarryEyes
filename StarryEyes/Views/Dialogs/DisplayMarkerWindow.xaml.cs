using System.Windows;
using StarryEyes.ViewModels.Dialogs;

namespace StarryEyes.Views.Dialogs
{
    /// <summary>
    /// DisplayMarkerWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class DisplayMarkerWindow : Window
    {
        public DisplayMarkerWindow()
        {
            InitializeComponent();
        }

        private void DisplayMarkerWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as DisplayMarkerViewModel;
            if (vm != null)
            {
                this.Left = vm.Left;
                this.Top = vm.Top;
            }
        }
    }
}
