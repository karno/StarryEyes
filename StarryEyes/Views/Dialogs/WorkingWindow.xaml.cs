using System;
using System.Threading.Tasks;
using System.Windows;

namespace StarryEyes.Views.Dialogs
{
    /// <summary>
    /// WorkingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class WorkingWindow : Window
    {
        private readonly Func<Action<string>, Task> _work;

        public WorkingWindow(string description, Func<Task> work)
            : this(description, _ => work())
        {

        }

        public WorkingWindow(string description, Func<Action<string>, Task> work)
        {
            _work = work;
            InitializeComponent();
            DetailText.Text = description;
            Loaded += DatabaseOptimizingWindow_Loaded;
        }

        async void DatabaseOptimizingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => _work(UpdateLabel)).ConfigureAwait(false);
            Close();
        }

        private void UpdateLabel(string label)
        {
            Dispatcher.InvokeAsync(() => DetailText.Text = label);
        }
    }
}
