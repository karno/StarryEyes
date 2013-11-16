using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            var window = Window.GetWindow(this);
            if (window == null || e.GetPosition(null).Y >= 48) return;
            try
            {
                window.DragMove();
            }
            catch (InvalidOperationException)
            {
                // failed start drag move.
            }
        }
    }
}
