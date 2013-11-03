using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
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
            // ensure create directory
            Directory.CreateDirectory(dir);
            using (var fstream = File.Create(fn))
            {
                await entry.Open().CopyToAsync(fstream);
            }
        }
    }

    public sealed class ExecuteAction : ReleaseActionBase
    {
        private readonly string _path;
        private readonly bool _awaitProcess;

        public ExecuteAction(string path, bool awaitProcess)
        {
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
            _path = path;
        }

        public override async Task DoWork(UpdateTaskExecutor executor)
        {
            executor.NotifyProgress("removing file: " + _path + " ...", false);
            await Task.Run(() => File.Delete(Path.Combine(executor.BasePath, _path)));
            executor.NotifyProgress("ok.");
        }
    }
}
