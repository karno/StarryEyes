using System;
using System.Reactive;
using System.Reactive.Linq;
using StarryEyes.Moon.Api.Rest;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;
using System.Windows.Media.Imaging;
using System.IO;
using StarryEyes.Settings;
namespace StarryEyes.Models.Operations
{
    public class TweetOperation : OperationBase<TwitterStatus>
    {
        public AuthenticateInfo AuthInfo { get; set; }

        public string Status { get; set; }

        public long InReplyTo { get; set; }

        public bool IsGeoLocationEnabled { get; set; }

        public double GeoLat { get; set; }

        public double GeoLong { get; set; }

        public bool IsImageAttachEnabled { get; set; }

        public byte[] AttachedImageBin { get; set; }

        private AuthenticateInfo _originalAuthInfo { get; set; }

        public BitmapSource AttachedImage
        {
            set
            {
                if (value == null) 
                {
                    AttachedImageBin = new byte[0];
                    return;
                }
                var ms = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(value));
                encoder.Save(ms);
                AttachedImageBin = ms.ToArray();
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
                                (cas = Setting.LookupAccountSetting(this.AuthInfo.Id)) != null &&
                                (fallbackAccount = Setting.LookupAccountSetting(cas.FallbackNext)) != null)
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
            if (IsGeoLocationEnabled)
            {
                geo_lat = this.GeoLat;
                geo_long = this.GeoLong;
            }
            if (IsImageAttachEnabled)
            {
                return AuthInfo.UpdateWithMedia(this.Status,
                    this.AttachedImageBin, "twitter_picture", false,
                    reply, geo_lat, geo_long);
            }
            else
            {
                return AuthInfo.Update(this.Status, reply, geo_lat, geo_long);
            }
        }
    }
}