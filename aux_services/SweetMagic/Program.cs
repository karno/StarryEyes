using System;
using System.Linq;
using System.Windows.Forms;

namespace SweetMagic
{
    public static class Program
    {
        public static string CallbackFile = "krile.exe";

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            var args = Environment.GetCommandLineArgs();
            if (args.Any(s => s.Equals("runas")))
            {
                Application.Run(new MainForm());
            }
            else
            {
                Application.Run(new Behind());
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            using (var sw = new System.IO.StreamWriter("upderr.txt"))
            {
                sw.WriteLine("CurrentDomain Unhandled:");
                sw.WriteLine(String.Join(" ", Environment.GetCommandLineArgs()));
                sw.WriteLine(e.ExceptionObject.ToString());
            }
            MessageBox.Show(
                "Fatal error has occured." + Environment.NewLine +
                "I wrote debug information in upderr.txt." + Environment.NewLine +
                "Please feedback this file to Krile Development Team.",
                "kup fatal error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            Application.Exit();
            Environment.Exit(-1);
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            using (var sw = new System.IO.StreamWriter("upderr.txt"))
            {
                sw.WriteLine("Application Thread:");
                sw.WriteLine(String.Join(" ", Environment.GetCommandLineArgs()));
                sw.WriteLine(e.Exception.ToString());
            }
            MessageBox.Show(
                "Fatal error has occured." + Environment.NewLine +
                "I wrote debug information in upderr.txt." + Environment.NewLine +
                "Please feedback this file to Krile Development Team.",
                "kup fatal error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            Application.Exit();
            Environment.Exit(-1);
        }
    }
}
