using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows;
using StarryEyes.Casket;
using StarryEyes.Nightmare.Windows;

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
            await Task.Run(async () =>
            {
                try
                {
                    await Database.VacuumTables();
                }
                catch (SQLiteException sqex)
                {
                    TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon = VistaTaskDialogIcon.Error,
                        Title = "Krile Database Optimization",
                        MainInstruction = "データベースの最適化に失敗しました。",
                        Content = sqex.Message,
                        ExpandedInfo = sqex.ToString()
                    });
                }
            });
            this.Close();
        }
    }
}
