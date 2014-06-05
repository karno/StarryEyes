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
            this._work = work;
            InitializeComponent();
            DetailText.Text = description;
            this.Loaded += DatabaseOptimizingWindow_Loaded;
        }

        async void DatabaseOptimizingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () => await this._work(UpdateLabel));
            this.Close();
        }

        private void UpdateLabel(string label)
        {
            Dispatcher.BeginInvoke(new Action(() => DetailText.Text = label));
        }
    }
}
