using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Helpers;
using StarryEyes.Models.Accounting;
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
        private readonly byte[] _attachedImageBin;

        public TweetPostingRequest(string status,
            TwitterStatus inReplyTo,
            GeoLocationInfo geoInfo,
            BitmapSource image)
            : this(status, inReplyTo == null ? (long?)null : inReplyTo.Id, geoInfo, image)
        {
        }

        public TweetPostingRequest(string status,
            long? inReplyTo,
            GeoLocationInfo geoInfo,
            BitmapSource image)
        {
            _status = status;
            _inReplyTo = inReplyTo;
            _geoInfo = geoInfo;
            if (image != null)
            {
                var ms = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                _attachedImageBin = ms.ToArray();
            }
            else
            {
                _attachedImageBin = null;
            }
        }

        public override async Task<TwitterStatus> Send(TwitterAccount account)
        {
            DebugHelper.EnsureBackgroundThread();
            var latlong = _geoInfo == null ? null : Tuple.Create(_geoInfo.Latitude, _geoInfo.Longitude);
            Exception thrown;
            // make retweet
            var acc = account;
            do
            {
                try
                {
                    if (_attachedImageBin != null)
                    {
                        return await acc.UpdateWithMedia(
                            _status,
                            new[] { _attachedImageBin },
                            account.IsMarkMediaAsPossiblySensitive ? true : (bool?)null, // Inherit property
                            _inReplyTo,
                            latlong);
                    }
                    return await acc.Update(
                        this._status,
                        this._inReplyTo,
                        latlong);
                }
                catch (TwitterApiException tae)
                {
                    thrown = tae;
                    if (tae.Message.Contains(LimitMessage) &&
                        acc.FallbackAccountId != null)
                    {
                        // reached post limit, fallback
                        acc = Setting.Accounts.Get(acc.FallbackAccountId.Value);
                        continue;
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
