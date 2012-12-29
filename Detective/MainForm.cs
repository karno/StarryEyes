using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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
            this.Opacity = 0.6;
            InitializeComponent();
            isSendFeedback.Checked = true;
            this.Load += OnFormLoad;
        }

        private async void OnFormLoad(object sender, EventArgs e)
        {
            for (int i = 0; i <= 10; i++)
            {
                this.Opacity = (double)i / 10;
                await Task.Run(() => Thread.Sleep(10));
            }
        }

        private async void DoClose()
        {
            for (int i = 10; i >= 0; i--)
            {
                this.Opacity = (double)i / 10;
                await Task.Run(() => Thread.Sleep(10));
            }
            this.Close();
        }

        private async void restartButton_Click(object sender, EventArgs e)
        {
            if (isSendFeedback.Checked)
                await Feedback();
            this.DoClose();
        }

        private async void exitButton_Click(object sender, EventArgs e)
        {
            if (isSendFeedback.Checked)
                await Feedback();
            this.DoClose();
        }

        private async Task Feedback()
        {
            this.Invoke(new Action(() => mainPanel.Visible = false));
            await Task.Run(() => Thread.Sleep(1000));
        }
    }
}
