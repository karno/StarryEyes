using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
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
                }
            }
        }

        private void inFile_TextChanged(object sender, EventArgs e)
        {
            var path = inFile.Text;
            if (!String.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                outFile.Text = Path.Combine(Path.GetDirectoryName(path),
                    Path.GetFileName(path) + ".ksz");
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

        private void generateSignFile_Click(object sender, EventArgs e)
        {
            var openFile = String.Empty;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "署名するファイルを選択";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    openFile = ofd.FileName;
                }
            }
            if (String.IsNullOrEmpty(openFile))
                return;
            var pkeyFile = String.Empty;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "private key|*.prv";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    pkeyFile = ofd.FileName;
                }
            }
            if (String.IsNullOrEmpty(pkeyFile))
                return;
            var write = openFile + ".sig";
            var sign = GetSignature(File.ReadAllBytes(openFile), File.ReadAllText(pkeyFile));
            File.WriteAllBytes(write, sign);
        }

        private byte[] GetSignature(byte[] bytes, String privateKey)
        {
            using (var sha = new SHA256Managed())
            using (var rsa = new RSACryptoServiceProvider())
            {
                // Compute hash
                var hash = sha.ComputeHash(bytes);
                // RSA Initialize
                rsa.FromXmlString(privateKey);
                // format
                var formatter = new RSAPKCS1SignatureFormatter(rsa);
                formatter.SetHashAlgorithm("SHA256");
                return formatter.CreateSignature(hash);
            }
        }

    }
}
