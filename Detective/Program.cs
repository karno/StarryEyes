using System;
using System.IO;
using System.Windows.Forms;

namespace Detective
{
    static class Program
    {
        public static string ErrorLogData { get; private set; }

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (Environment.GetCommandLineArgs().Length >= 2)
            {
                var logFilePath = Environment.GetCommandLineArgs()[1];
                if (File.Exists(logFilePath) && Path.GetExtension(logFilePath) == ".crashlog")
                {
                    try
                    {
                        ErrorLogData = File.ReadAllText(logFilePath);
                        ErrorLogData += Environment.NewLine + Environment.OSVersion.VersionString;
                        Application.Run(new MainForm());
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "ログファイルの読み取りに失敗しました。" + Environment.NewLine +
                            "エラー:" + ex.Message, "レポーター エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            MessageBox.Show(
                "このソフトウェアはエラーレポートのためのソフトウェアです。" + Environment.NewLine +
                "Krileを使うには、krile.exe を起動してください。",
                "レポーター", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }
}
