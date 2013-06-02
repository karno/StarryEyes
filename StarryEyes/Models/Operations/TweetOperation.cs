using System;
using System.IO;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Backstages.NotificationEvents.PostEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Operations
{
    public class TweetOperation : OperationBase<TwitterStatus>
    {
        public TweetOperation() { }

        public TweetOperation(AuthenticateInfo info,
            string status,
            TwitterStatus inReplyTo,
            GeoLocationInfo geoLocation,
            BitmapSource image)
        {
            this.AuthInfo = info;
            this.Status = status;
            this.InReplyTo = inReplyTo != null ? inReplyTo.Id : 0;
            this.GeoLocation = geoLocation;
            this.AttachedImage = image;
        }

        public AuthenticateInfo AuthInfo { get; set; }

        public string Status { get; set; }

        public long InReplyTo { get; set; }

        public GeoLocationInfo GeoLocation { get; set; }

        private AuthenticateInfo OriginalAuthInfo { get; set; }

        private byte[] _attachedImageBin;
        private BitmapSource _originalBitmapSource;
        public BitmapSource AttachedImage
        {
            get
            {
                return _originalBitmapSource;
            }
            set
            {
                _originalBitmapSource = value;
                if (value == null)
                {
                    _attachedImageBin = new byte[0];
                    return;
                }
                var ms = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(value));
                encoder.Save(ms);
                _attachedImageBin = ms.ToArray();
            }
        }

        protected override IObservable<TwitterStatus> RunCore()
        {
            return ExecPost()
                .Catch((Exception ex) =>
                       GetExceptionDetail(ex)
                           .SelectMany(s =>
                           {
                               AccountSetting cas;
                               AccountSetting fallbackAccount;
                               if (s.Contains("over daily status update limit") &&
                                   (cas = AccountsStore.GetAccountSetting(this.AuthInfo.Id)) != null &&
                                   (fallbackAccount = AccountsStore.GetAccountSetting(cas.FallbackNext)) != null)
                               {
                                   // Post limit, go fallback
                                   if (this.OriginalAuthInfo == null)
                                   {
                                       this.OriginalAuthInfo = AuthInfo;
                                   }
                                   var source = this.AuthInfo;
                                   this.AuthInfo = fallbackAccount.AuthenticateInfo;
                                   BackstageModel.RegisterEvent(new FallbackedEvent(source, this.AuthInfo));
                                   return this.ExecPost();
                               }
                               return Observable.Throw<TwitterStatus>(new TweetFailedException(s, ex));
                           }));
        }

        private IObservable<TwitterStatus> ExecPost()
        {
            var reply = InReplyTo == 0 ? null : (long?)InReplyTo;
            double? geoLat = null;
            double? geoLong = null;
            if (GeoLocation != null)
            {
                geoLat = GeoLocation.Latitude;
                geoLong = GeoLocation.Longitude;
            }
            if (AttachedImage != null)
            {
                return AuthInfo.UpdateWithMedia(this.Status, this._attachedImageBin, "picture.png", geo_lat: geoLat, geo_long: geoLong);
            }
            return AuthInfo.Update(this.Status, reply, geoLat, geoLong);
        }
    }

    public class GeoLocationInfo
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public override string ToString()
        {
            return "lat:" + Latitude.ToString("0.000") + ", long:" + Longitude.ToString("0.000");
        }
    }

    [Serializable]
    public class TweetFailedException : Exception
    {
        public TweetFailedException(string message, Exception inner) : base(message, inner) { }
        protected TweetFailedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}