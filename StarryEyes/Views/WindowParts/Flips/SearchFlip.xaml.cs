using System.Windows;
using System.Windows.Controls;
using StarryEyes.ViewModels.WindowParts.Flips;

namespace StarryEyes.Views.WindowParts.Flips
{
    /// <summary>
    /// SearchFlip.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchFlip : UserControl
    {
        public SearchFlip()
        {
            InitializeComponent();
        }

        private void ResultGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var width = ResultGrid.ActualWidth;
            var vm = ResultGrid.DataContext as SearchFlipViewModel;
            if (vm != null)
            {
                vm.NotifyResultWidthChanged(width);
            }
        }
    }
}
