using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace SweetMagic
{
    public static class ReleaseActionFactory
    {
        public static ReleaseActionBase Parse(XElement element)
        {
            switch (element.Name.LocalName.ToLower())
            {
                case "pack":
                case "package":
                    return new PackageAction(element.Attribute("url").Value);
                case "exec":
                case "execute":
                    var ap = element.Attribute("await"); // default true
                    return new ExecuteAction(element.Attribute("path").Value,
                                             ap == null || ap.Value.ToLower() != "false");
                case "delete":
                    return new DeleteAction(element.Attribute("path").Value);
                default:
                    throw new UpdateException("update action is not valid: " + element.Name);
            }
        }
    }

    public abstract class ReleaseActionBase
    {
        public abstract Task DoWork(UpdateTaskExecutor executor);

        protected void AssertPath(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (path.Contains("..") || path.Contains("%") ||
                Path.GetInvalidPathChars().Any(path.Contains))
            {
                // invalid path
                throw new ArgumentException("Invalid path spcified.");
            }
        }
    }

    public sealed class PackageAction : ReleaseActionBase
    {
        public PackageAction(string url)
        {
            this.Url = url;
        }

        public string Url { get; set; }

        public async override Task DoWork(UpdateTaskExecutor executor)
        {
            executor.NotifyProgress("downloading patch package...");
            byte[] file = null;
            for (var i = 0; i < 3; i++)
            {
                file = await executor.DownloadBinary(this.Url);
                if (file != null) break;
                executor.NotifyProgress("<!> patch package download failed. awaiting server a few seconds...", false);
                await Task.Run(() => Thread.Sleep(10000));
                executor.NotifyProgress("retrying download patch package...");
            }
            if (file == null)
            {
                executor.NotifyProgress("***** FATAL: patch package download failed! *****");
                throw new UpdateException("patch package download failed.");
            }
            using (var ms = new MemoryStream(file))
            using (var ss = new MemoryStream())
            {
                executor.NotifyProgress("verifying patch pack...", false);
                if (!Cryptography.Verify(ms, ss, executor.PublicKey))
                {
                    executor.NotifyProgress("Invalid signature.");
                    throw new UpdateException("patch package signature is not valid.");
                }
                executor.NotifyProgress("verified.");
                executor.NotifyProgress("applying patches.");
                var archive = new ZipArchive(ss);
                foreach (var entry in archive.Entries)
                {
                    executor.NotifyProgress("patching " + entry.Name + "...");
                    await this.Extract(entry, executor.BasePath);
                }
                executor.NotifyProgress("patch completed.");
            }
        }

        private async Task Extract(ZipArchiveEntry entry, string basePath)
        {
            var fn = Path.Combine(basePath, entry.FullName);
            var dir = Path.GetDirectoryName(fn);
            if (dir == null)
            {
                throw new UpdateException("patch path is not valid.");
            }
            try
            {
                // ensure create directory
                Directory.CreateDirectory(dir);
                // remove previous file
                if (File.Exists(fn))
                {
                    File.Delete(fn);
                }
                using (var fstream = File.Create(fn))
                {
                    await entry.Open().CopyToAsync(fstream);
                }
                // complete.
                return;
            }
            catch (Exception ex)
            {
                var resp = MessageBox.Show(
                    "Could not extract file: " + entry.FullName + Environment.NewLine +
                    ex.Message + Environment.NewLine +
                    "Would you like to retry?",
                    "Krile Automatic Update",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Error);
                if (resp == DialogResult.Cancel)
                {
                    // cancelled.
                    throw new UpdateException("Failed to apply patch.");
                }
            }
            // failed, retrying...
            await Extract(entry, basePath);
        }
    }

    public sealed class ExecuteAction : ReleaseActionBase
    {
        private readonly string _path;
        private readonly bool _awaitProcess;

        public ExecuteAction(string path, bool awaitProcess)
        {
            AssertPath(path);
            _path = path;
            _awaitProcess = awaitProcess;
        }

        public override async Task DoWork(UpdateTaskExecutor executor)
        {
            executor.NotifyProgress("executing binary: " + _path);
            var process = Process.Start(Path.Combine(executor.BasePath, _path));
            if (_awaitProcess && process != null)
            {
                executor.NotifyProgress("waiting exit...", false);
                await Task.Run(() => process.WaitForExit());
                executor.NotifyProgress("ok.");
            }
        }
    }

    public sealed class DeleteAction : ReleaseActionBase
    {
        private readonly string _path;

        public DeleteAction(string path)
        {
            AssertPath(path);
            _path = path;
        }

        public override async Task DoWork(UpdateTaskExecutor executor)
        {
            if (_path.EndsWith("/"))
            {
                // directory
                var target = Path.Combine(executor.BasePath, _path.Substring(0, _path.Length - 1));
                if (Directory.Exists(target))
                {
                    executor.NotifyProgress("removing directory: " + _path + " ...", false);
                    await Task.Run(() => Directory.Delete(target, true));
                }
                else
                {
                    executor.NotifyProgress("directory " + _path + " is not existed. nothing to do.");
                }
            }
            else
            {
                // file
                var target = Path.Combine(executor.BasePath, _path);
                if (File.Exists(target))
                {
                    executor.NotifyProgress("removing file: " + _path + " ...", false);
                    await Task.Run(() => File.Delete(target));
                }
                else
                {
                    executor.NotifyProgress("file " + _path + " is not existed. nothing to do.");
                }
            }
            executor.NotifyProgress("ok.");
        }
    }
}
