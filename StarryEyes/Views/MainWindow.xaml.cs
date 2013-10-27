using System;
using System.Reactive.Linq;
using System.Windows.Input;
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

            Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
                      .ObserveOnDispatcher()
                      .Subscribe(_ =>
                      {
                          var focusElement = Keyboard.FocusedElement;
                          System.Diagnostics.Debug.WriteLine("focus: " + (focusElement == null ? "null" : focusElement.GetType().ToString()));
                      });
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
