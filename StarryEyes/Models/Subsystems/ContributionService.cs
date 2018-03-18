using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Livet;
using StarryEyes.Settings;

namespace StarryEyes.Models.Subsystems
{
    internal static class ContributionService
    {
        internal static ObservableSynchronizedCollectionEx<Contributor> Contributors { get; } =
            new ObservableSynchronizedCollectionEx<Contributor>();

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
                        return xml.Root?
                                  .Descendants("contributor")
                                  .Where(
                                      e =>
                                          e.Attribute("visible") == null ||
                                          e.Attribute("visible")?.Value.ToLower() != "false")
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

        public static bool IsContributor()
        {
            var users = Contributors.Select(c => c.ScreenName).ToArray();
            return Setting.Accounts.Collection.Any(c => users.Contains(c.UnreliableScreenName));
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
            Name = name;
            ScreenName = screenName;
        }

        public string Name { get; }

        public string ScreenName { get; }
    }
}