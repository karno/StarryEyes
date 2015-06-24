using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents.PostEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Requests
{
    public sealed class TweetPostingRequest : RequestBase<TwitterStatus>
    {
        public override int RetryCount
        {
            get { return 0; }
        }

        public override double RetryDelaySec
        {
            get { return 0; }
        }

        private const string LimitMessage = "over daily status update limit";
        private readonly string _status;
        private readonly long? _inReplyTo;
        private readonly GeoLocationInfo _geoInfo;
        private readonly IList<byte[]> _attachedImages;

        public TweetPostingRequest(string status,
            TwitterStatus inReplyTo, GeoLocationInfo geoInfo,
            IEnumerable<byte[]> attachedImages)
            : this(status, inReplyTo == null ? (long?)null : inReplyTo.Id,
                geoInfo, attachedImages)
        {
        }

        public TweetPostingRequest(string status, long? inReplyTo,
            GeoLocationInfo geoInfo, IEnumerable<byte[]> attachedImages)
        {
            _status = status;
            _inReplyTo = inReplyTo;
            _geoInfo = geoInfo;
            _attachedImages = attachedImages == null ? null : attachedImages.ToArray();
        }

        public override async Task<IApiResult<TwitterStatus>> Send(TwitterAccount account)
        {
            var latlong = _geoInfo == null ? null : Tuple.Create(_geoInfo.Latitude, _geoInfo.Longitude);
            Exception thrown;
            // make retweet
            var acc = account;
            do
            {
                try
                {
                    var param = new StatusParameter(
                        _status, _inReplyTo,
                        account.MarkMediaAsPossiblySensitive ? true : (bool?)null,
                        latlong);
                    if (_attachedImages != null)
                    {
                        var ids = new List<long>();
                        foreach (var img in _attachedImages)
                        {
                            ids.Add((await acc.UploadMediaAsync(ApiAccessProperties.DefaultForUpload, img)).Result);
                        }
                        param.MediaIds = ids.ToArray();
                    }
                    var result = await acc.UpdateAsync(ApiAccessProperties.Default, param);
                    BackstageModel.NotifyFallbackState(acc, false);
                    return result;
                }
                catch (TwitterApiException tae)
                {
                    thrown = tae;
                    if (tae.Message.Contains(LimitMessage))
                    {
                        BackstageModel.NotifyFallbackState(acc, true);
                        if (acc.FallbackAccountId != null)
                        {
                            // reached post limit, fallback
                            var prev = acc;
                            acc = Setting.Accounts.Get(acc.FallbackAccountId.Value);
                            BackstageModel.RegisterEvent(new FallbackedEvent(prev, acc));
                            continue;
                        }
                    }
                }
                throw thrown;
            } while (acc != null && acc.Id != account.Id);
            throw thrown;
        }
    }

    public sealed class GeoLocationInfo
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public override string ToString()
        {
            return "lat:" + Latitude.ToString("0.000") + ", long:" + Longitude.ToString("0.000");
        }
    }
}
