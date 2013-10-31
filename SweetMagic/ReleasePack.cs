using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SweetMagic
{
    public class ReleasePack
    {
        public static ReleasePack Parse(string xml)
        {
            var doc = XDocument.Parse(xml);
            if (doc.Root == null || doc.Root.Name != "releases")
            {
                throw new ArgumentException("Release pack xml is not valid.");
            }
            return new ReleasePack
            {
                Timestamp = DateTime.Parse(doc.Root.Attribute("date").Value),
                Releases = doc.Root.Elements("release").Select(r => new Release(r)).ToArray()
            };
        }

        public DateTime Timestamp { get; private set; }

        public IEnumerable<Release> Releases { get; private set; }

        public IEnumerable<Release> GetPatchesShouldBeApplied(Version currentVersion, bool acceptPreviewVersion)
        {
            // get differential patches
            var patches = Releases.OrderBy(r => r.Version)
                                  .SkipWhile(r => r.Version <= currentVersion)
                                  .Where(p => acceptPreviewVersion || p.Version.Revision <= 0) // check release channel
                                  .ToArray();
            if (patches.Any(p => p.IsMilestone))
            {
                // get latest milestone patches and patches after it
                var afterms = false;
                return patches.Reverse().Where(p =>
                {
                    if (afterms)
                    {
                        return !p.CanSkip;
                    }
                    if (p.IsMilestone)
                    {
                        afterms = true;
                    }
                    return true;
                }).Reverse();
            }
            return patches;
        }
    }

    public class Release
    {
        public Version Version { get; private set; }

        public bool IsMilestone { get; private set; }

        public DateTime ReleaseTime { get; private set; }

        public bool CanSkip { get; private set; }

        public IEnumerable<ReleaseActionBase> Actions { get; private set; }

        public Release(XElement element)
        {
            this.Version = Version.Parse(element.Attribute("version").Value);
            var milestone = element.Attribute("milestone");
            this.IsMilestone = milestone != null && Boolean.Parse(milestone.Value);
            this.ReleaseTime = DateTime.Parse(element.Attribute("date").Value);
            var skippable = element.Attribute("skippable");
            this.CanSkip = skippable == null || Boolean.Parse(skippable.Value);
            this.Actions = element.Elements().Select(ReleaseActionFactory.Parse).ToArray();
        }
    }
}
