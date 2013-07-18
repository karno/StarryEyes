using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ripple
{
    public static class ReleaseActionFactory
    {
        public static ReleaseActionBase Parse(XElement element)
        {
            switch (element.Value)
            {
                case "pack":
                case "package":
                    return new PackageAction(element.Attribute("url").Value);
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
            var file = await executor.DownloadBinary(Url);
            using (var ms = new MemoryStream(file))
            using (var ss = new MemoryStream())
            {
                if (!Cryptography.Verify(ms, ss, executor.PublicKey))
                {
                    executor.NotifyProgress("Invalid signature.");
                    throw new Exception("Package signature is not matched.");
                }
                var archive = new ZipArchive(ss);
                foreach (var entry in archive.Entries)
                {
                    await this.Extract(entry, executor.BasePath);
                }
            }
        }

        private async Task Extract(ZipArchiveEntry entry, string basePath)
        {
            var fn = Path.Combine(basePath, entry.FullName);
            using (var fstream = File.Create(fn))
            {
                await entry.Open().CopyToAsync(fstream);
            }
        }
    }
}
