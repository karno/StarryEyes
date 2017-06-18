using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Detective
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            this.Opacity = 0.2;
            InitializeComponent();
            isSendFeedback.Checked = true;
            this.Load += OnFormLoad;
        }

        private async void OnFormLoad(object sender, EventArgs e)
        {
            errorText.Text = Program.ErrorLogData;
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

        private async void restartButton_Click(object sender, EventArgs e)
        {
            if (isSendFeedback.Checked)
            {
                if (!await Feedback())
                {
                    return;
                }
            }
            var apppath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            var psi = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Path.Combine(apppath, Program.ParentExeName)
            };
            Process.Start(psi);
            this.DoClose();
        }

        private async void exitButton_Click(object sender, EventArgs e)
        {
            if (isSendFeedback.Checked)
            {
                if (!await Feedback())
                {
                    return;
                }
            }
            this.DoClose();
        }

        private const string FeedbackUri = "http://krile.starwing.net/shared/report.php";
        private async Task<bool> Feedback()
        {
            mainPanel.Visible = false;
            try
            {
                await Task.Run(() => this.PostString(new Uri(FeedbackUri), Program.ErrorLogData));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Feedback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private void PostString(Uri target, string data)
        {
            var post = "error=" + Uri.EscapeDataString(data);
            var bytes = Encoding.UTF8.GetBytes(post);

            var req = WebRequest.Create(target);
            req.Proxy = WebRequest.DefaultWebProxy;

            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = bytes.Length;

            //データをPOST送信するためのStreamを取得
            using (var rs = req.GetRequestStream())
            {
                rs.Write(bytes, 0, bytes.Length);
            }

            using (var res = (HttpWebResponse)req.GetResponse())
            {
                if (res.StatusCode != HttpStatusCode.OK)
                {
                    throw new WebException(res.StatusDescription);
                }
            }
        }
    }
}
