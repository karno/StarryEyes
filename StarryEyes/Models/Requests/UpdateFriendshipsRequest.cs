using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    public class UpdateFriendshipsRequest : RequestBase<TwitterFriendship>
    {
        private readonly long _userId;
        private readonly bool? _deviceNotifications;
        private readonly bool? _showRetweets;

        public UpdateFriendshipsRequest(TwitterUser target, bool? deviceNotifications, bool? showRetweets)
            : this(target.Id, deviceNotifications, showRetweets)
        {
        }

        public UpdateFriendshipsRequest(long userId, bool? deviceNotifications, bool? showRetweets)
        {
            _userId = userId;
            _deviceNotifications = deviceNotifications;
            _showRetweets = showRetweets;
        }

        public override Task<TwitterFriendship> Send(TwitterAccount account)
        {
            return account.UpdateFriendshipAsync(_userId, _deviceNotifications, _showRetweets);
        }
    }
}
