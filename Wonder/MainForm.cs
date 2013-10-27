using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using SweetMagic;

namespace Wonder
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void generateKeys_Click(object sender, EventArgs e)
        {
            string pubkey = null;
            string prvkey = null;
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "public key|*.pub";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    pubkey = sfd.FileName;
                }
            }
            if (pubkey == null) return;
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "private key|*.prv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    prvkey = sfd.FileName;
                }
            }
            if (prvkey == null) return;
            string pubkeybody;
            string prvkeybody;
            Cryptography.CreateKeys(out pubkeybody, out prvkeybody);
            File.WriteAllText(pubkey, pubkeybody);
            File.WriteAllText(prvkey, prvkeybody);
        }

        private void openDirectory_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (!String.IsNullOrEmpty(inFile.Text))
                {
                    fbd.SelectedPath = inFile.Text;
                }
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    inFile.Text = fbd.SelectedPath;
                    outFile.Text = Path.Combine(Path.GetDirectoryName(fbd.SelectedPath), "archive.ksz");
                }
            }
        }

        private void saveFile_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                if (!String.IsNullOrEmpty(outFile.Text))
                {
                    sfd.InitialDirectory = Path.GetDirectoryName(outFile.Text);
                    sfd.FileName = outFile.Text;
                }
                sfd.Filter = "Krile Signatured Zip|*.ksz";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    outFile.Text = sfd.FileName;
                }
            }
        }

        private void openKeyFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                ofd.Filter = "private key|*.prv";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    keyFile.Text = ofd.FileName;
                }
            }
        }

        private async void generatePackage_Click(object sender, EventArgs e)
        {
            using (var ms = new MemoryStream())
            using (var fs = new FileStream(outFile.Text, FileMode.Create, FileAccess.ReadWrite))
            {
                var tlen = inFile.Text.Length;
                if (!inFile.Text.EndsWith("\\"))
                {
                    tlen++;
                }
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    foreach (var file in Directory.GetFiles(inFile.Text, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = file.Substring(tlen);
                        var entry = archive.CreateEntry(relativePath);
                        using (var estream = entry.Open())
                        using (var ifs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            await ifs.CopyToAsync(estream);
                        }
                    }
                }
                // reset seek point of destination stream
                ms.Seek(0, SeekOrigin.Begin);
                Cryptography.Signature(ms, fs, File.ReadAllText(keyFile.Text));
            }
            MessageBox.Show("completed.", "Krile archive generator", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
