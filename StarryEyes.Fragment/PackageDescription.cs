using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace StarryEyes.Feather.Packaging
{
    public class PackageDescription
    {
        public PackageDescription() { }

        public PackageDescription(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                var xml = XDocument.Load(fs);
                var pack = xml.Element("package");
                this.Name = pack.Element("name").Value;
                this.PluginVersion = new Version(pack.Element("version").Value);
                var supported = pack.Element("supported");
                this.SupportedMaxVersion = new Version(supported.Element("max").Value);
                this.SupportedMinVersion = new Version(supported.Element("min").Value);
                this.UpdateFromUri = new Uri(pack.Element("update").Value);
                this.Binaries = pack.Element("binaries")
                    .Elements("entry")
                    .Select(_ => _.Value)
                    .ToArray();
            }
        }

        public string Name { get; private set; }

        public Version PluginVersion { get; private set; }

        public Version SupportedMaxVersion { get; private set; }

        public Version SupportedMinVersion { get; private set; }

        public Uri UpdateFromUri { get; private set; }

        public IEnumerable<string> Binaries { get; private set; }

        public string GetSignatureFilePathForBinary(string file)
        {
            return file + ".sig";
        }

        public void WriteDescription(string path)
        {
            using (var fs = File.OpenWrite(path))
            {
                var xml = new XDocument();
                var pack = new XElement("package");
                pack.Add(new XElement("name", this.Name));
                pack.Add(new XElement("version", this.PluginVersion.ToString()));
                var max = new XElement("max", this.SupportedMaxVersion.ToString());
                var min = new XElement("min", this.SupportedMinVersion.ToString());
                pack.Add(new XElement("supported", max, min));
                pack.Add(new XElement("update", this.UpdateFromUri.OriginalString));
                pack.Add(new XElement("pubkey", this.Binaries.Select(_ => new XElement("entry", _)).ToArray()));
                xml.Add(pack);
                xml.Save(fs);
            }
        }
    }
}
