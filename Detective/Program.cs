using System;
using System.IO;
using System.Windows.Forms;

namespace Detective
{
    public static class Program
    {
        public static string ErrorLogData { get; private set; }

        public static string ParentExeName = "krile.exe";

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
                        if (SelfAnalyzer.Analyze(ErrorLogData))
                        {
                            Application.Run(new SelfAnalyzedForm());
                        }
                        else
                        {
                            Application.Run(new MainForm());
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "ログファイルの読み取りに失敗しました。" + Environment.NewLine +
                            "エラー:" + ex.Message, "Krile Error Reporter",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            MessageBox.Show(
                "このソフトウェアはエラーレポートのためにKrileから利用されます。" + Environment.NewLine +
                "Krileを使うには、krile.exe を起動してください。",
                "Krile Error Reporter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }
}
