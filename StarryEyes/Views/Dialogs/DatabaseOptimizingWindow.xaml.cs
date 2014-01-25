using System.Threading.Tasks;
using System.Windows;
using StarryEyes.Casket;

namespace StarryEyes.Views.Dialogs
{
    /// <summary>
    /// DatabaseOptimizingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class DatabaseOptimizingWindow : Window
    {
        public DatabaseOptimizingWindow()
        {
            InitializeComponent();
            this.Loaded += DatabaseOptimizingWindow_Loaded;
        }

        async void DatabaseOptimizingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () => await Database.VacuumTables());
            this.Close();
        }
    }
}
