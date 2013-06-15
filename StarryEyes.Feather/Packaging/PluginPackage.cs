using System;
using System.IO;
using System.IO.Compression;

namespace StarryEyes.Feather.Packaging
{
    internal class PluginPackage
    {
        public const string PluginDescriptionFile = "package.xml";

        public const string PluginPublicKeyFile = "key.pub";

        public PluginPackage(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Plugin package file is not found. :" + path);
            this.PluginPackagePath = path;
            this.IsExtracted = false;
        }

        public void ExtractTo(string path)
        {
            using (var zip = ZipFile.OpenRead(this.PluginPackagePath))
            {
                zip.ExtractToDirectory(path);
            }
            var descfile = Path.Combine(path, PluginDescriptionFile);
            if (File.Exists(descfile))
            {
                this.Description = new PackageDescription(descfile);
                this.IsExtracted = true;
            }
            else
            {
                throw new ArgumentException(
                    "description file is not found in plugin package.(" + PluginPackagePath + ")");
            }
            var sigfile = Path.Combine(path, PluginPublicKeyFile);
            if (File.Exists(sigfile))
            {

            }
        }

        public string PluginPackagePath { get; private set; }

        public bool IsExtracted { get; private set; }

        public PackageDescription Description { get; private set; }

        public string SignatureFilePath { get; private set; }

        public string AuthoritySignatureFilePath { get; private set; }
    }
}
