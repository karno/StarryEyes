using System;
using System.IO;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using StarryEyes.Models.Store;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
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
            this.GeoLocation = GeoLocation;
            this.AttachedImage = image;
        }

        public AuthenticateInfo AuthInfo { get; set; }

        public string Status { get; set; }

        public long InReplyTo { get; set; }

        public GeoLocationInfo GeoLocation { get; set; }

        private AuthenticateInfo _originalAuthInfo { get; set; }

        private byte[] attachedImageBin;
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
                    attachedImageBin = new byte[0];
                    return;
                }
                var ms = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(value));
                encoder.Save(ms);
                attachedImageBin = ms.ToArray();
            }
        }

        protected override IObservable<TwitterStatus> RunCore()
        {
            return ExecPost()
                .Catch((Exception ex) =>
                {
                    return
                        GetExceptionDetail(ex)
                        .SelectMany(s =>
                        {
                            AccountSetting cas;
                            AccountSetting fallbackAccount;
                            if (s.Contains("User is over daily status update limit.") &&
                                (cas = AccountsStore.GetAccountSetting(this.AuthInfo.Id)) != null &&
                                (fallbackAccount = AccountsStore.GetAccountSetting(cas.FallbackNext)) != null)
                            {
                                // Post limit, go fallback
                                if (this._originalAuthInfo != null)
                                    this._originalAuthInfo = AuthInfo;
                                this.AuthInfo = fallbackAccount.AuthenticateInfo;
                                return this.Run(OperationPriority.High);
                            }
                            else
                            {
                                return Observable.Throw<TwitterStatus>(ex);
                            }
                        });
                });
        }

        private IObservable<TwitterStatus> ExecPost()
        {
            long? reply = InReplyTo == 0 ? null : (long?)InReplyTo;
            double? geo_lat = null;
            double? geo_long = null;
            if (GeoLocation != null)
            {
                geo_lat = GeoLocation.Latitude;
                geo_long = GeoLocation.Longitude;
            }
            if (AttachedImage != null)
            {
                return Setting.GetImageUploader()
                    .Upload(this.AuthInfo, this.Status,
                    this.attachedImageBin, reply, geo_lat, geo_long);
            }
            else
            {
                return AuthInfo.Update(this.Status, reply, geo_lat, geo_long);
            }
        }
    }

    public class GeoLocationInfo
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}