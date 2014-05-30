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
                    Title = "Auto update error",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "Update agent could not be downloaded.",
                    Content = "Update server is down, or patches are not available.",
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
