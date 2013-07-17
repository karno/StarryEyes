using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ripple
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            var exec = new UpdateTaskExecutor();
            exec.OnNotifyProgress += str => this.Invoke(new Action(() => this.logField.AppendText(str)));
            Task.Run((Action)exec.StartUpdate);
        }
    }
}
