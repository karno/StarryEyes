using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SweetMagic
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            // kup.exe [ver_str] [pubkey path] [base path] [process id] [runas]
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            if (args.Length < 5)
            {
                MessageBox.Show("Invalid argument." + Environment.NewLine +
                                Environment.GetCommandLineArgs(),
                                "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                Environment.Exit(-1);
            }
            var ver = Version.Parse(args[0]);
            var pubkey = File.ReadAllText(args[1]);
            var exec = new UpdateTaskExecutor(ver, pubkey, args[2], int.Parse(args[3]), args[0].Count(c => c == '.') == 3);
            exec.OnNotifyProgress += str => this.Invoke(new Action(() => this.logField.AppendText(str)));
            Task.Run(async () =>
            {
                try
                {
                    if (!await exec.StartUpdate())
                    {
                        Application.Exit();
                        Environment.Exit(-1);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fatal error occured." + Environment.NewLine + ex);
                    Application.Exit();
                    Environment.Exit(-1);
                }
                Application.Exit();
                Environment.Exit(0);
            });
        }
    }
}
