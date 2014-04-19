using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Livet;

namespace StarryEyes.Models.Subsystems
{
    internal static class ContributionService
    {
        private static readonly ObservableSynchronizedCollectionEx<Contributor> _contributors =
            new ObservableSynchronizedCollectionEx<Contributor>();

        internal static ObservableSynchronizedCollectionEx<Contributor> Contributors
        {
            get { return _contributors; }
        }

        static ContributionService()
        {
            Observable.Timer(TimeSpan.FromSeconds(0), TimeSpan.FromHours(8))
                      .Subscribe(_ => Task.Run(async () => await UpdateContributors()));
        }

        private static async Task UpdateContributors()
        {
            try
            {
                var vms = await Task.Run(async () =>
                {
                    var hc = new HttpClient();
                    var str = await hc.GetStringAsync(App.ContributorsUrl);
                    using (var sr = new StringReader(str))
                    {
                        var xml = XDocument.Load(sr);
                        return xml.Root
                                  .Descendants("contributor")
                                  .Where(
                                      e =>
                                      e.Attribute("visible") == null ||
                                      e.Attribute("visible").Value.ToLower() != "false")
                                  .Select(Contributor.FromXml)
                                  .ToArray();
                    }
                });
                Contributors.Clear();
                vms.OrderBy(v => v.ScreenName ?? "~" + v.Name)
                   .ForEach(Contributors.Add);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }

    public class Contributor
    {
        public static Contributor FromXml(XElement xElement)
        {
            var twitter = xElement.Attribute("twitter");
            return twitter != null
                       ? new Contributor(xElement.Value, twitter.Value)
                       : new Contributor(xElement.Value, null);
        }

        public Contributor(string name, string screenName)
        {
            this.Name = name;
            this.ScreenName = screenName;
        }

        public string Name { get; set; }

        public string ScreenName { get; set; }
    }
}
