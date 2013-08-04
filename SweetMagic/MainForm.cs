using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SweetMagic
{
    public partial class MainForm : Form
    {
        readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            //todo: fill parameters
            var exec = new UpdateTaskExecutor("PUBLIC KEY", "BASEPATH");
            exec.OnNotifyProgress += str => this.Invoke(new Action(() => this.logField.AppendText(str)));
            Task.Run(() => exec.StartUpdate(this._cancelSource.Token));
        }
    }
}
