using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Detective
{
    public partial class SelfAnalyzedForm : Form
    {
        public SelfAnalyzedForm()
        {
            this.Opacity = 0.2;
            InitializeComponent();
            this.Load += OnFormLoad;
        }

        private async void OnFormLoad(object sender, EventArgs e)
        {
            errorText.Text = SelfAnalyzer.AnalyzeResult + Environment.NewLine + Program.ErrorLogData;
            for (var i = 0; i <= 10; i++)
            {
                this.Opacity = (double)i / 10;
                await Task.Run(() => Thread.Sleep(10));
            }
        }

        private async void DoClose()
        {
            for (var i = 10; i >= 0; i--)
            {
                this.Opacity = (double)i / 10;
                await Task.Run(() => Thread.Sleep(10));
            }
            this.Close();
        }

        private void restartButton_Click(object sender, EventArgs e)
        {
            var apppath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            var psi = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Path.Combine(apppath, Program.ParentExeName)
            };
            Process.Start(psi);
            this.DoClose();
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            this.DoClose();
        }
    }
}
