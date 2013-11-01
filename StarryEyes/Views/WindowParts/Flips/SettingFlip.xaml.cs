using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StarryEyes.Views.WindowParts.Flips
{
    /// <summary>
    /// SettingFlip.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingFlip : UserControl
    {
        public SettingFlip()
        {
            InitializeComponent();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if(e.GetPosition(null).Y < 48)
                Window.GetWindow(this).DragMove();
        }
    }
}
