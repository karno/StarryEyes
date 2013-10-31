using System.Windows;
using StarryEyes.Models.Subsystems;
using StarryEyes.Nightmare.Windows;

namespace StarryEyes.Views.Dialogs
{
    /// <summary>
    /// AwaitDownloadingUpdateWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AwaitDownloadingUpdateWindow : Window
    {
        public AwaitDownloadingUpdateWindow()
        {
            InitializeComponent();
            this.Loaded += AwaitDownloadingUpdateWindow_Loaded;
        }

        async void AwaitDownloadingUpdateWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!await AutoUpdateService.CheckPrepareUpdate(null))
            {
                TaskDialog.Show(new TaskDialogOptions
                {
                    Title = "自動アップデート エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "アップデートパッチをダウンロードできませんでした。",
                    Content = "アップデート サーバが利用可能でないか、利用可能なパッチが存在しません。",
                    CommonButtons = TaskDialogCommonButtons.Close
                });
                this.Close();
                return;
            }
            AutoUpdateService.StartUpdate(null);
            this.Close();
        }
    }
}
