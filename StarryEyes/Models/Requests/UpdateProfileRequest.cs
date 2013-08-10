using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    public sealed class UpdateProfileRequest : RequestBase<TwitterUser>
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string Location { get; set; }

        public string DescriptionText { get; set; }

        public override Task<TwitterUser> Send(TwitterAccount account)
        {
            return account.UpdateProfile(Name, Url, Location, DescriptionText);
        }
    }
}
