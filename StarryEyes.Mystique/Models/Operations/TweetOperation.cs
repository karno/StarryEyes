using System;
using System.Reactive;
using System.Reactive.Linq;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.SweetLady.DataModel;
using System.Windows.Media.Imaging;
using System.IO;
namespace StarryEyes.Mystique.Models.Operations
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
                    /* fallback logic, unimplemented
                    return
                        GetExceptionDetail(ex)
                        .Select(s =>
                        {
                            long? fallbackId;
                            AuthenticateInfo fallbackAccount;
                            if (s.Contains("User is over daily status update limit.") &&
                                (fallbackId = Setting.GetRelatedInfo(this.AuthInfo).FallbackNext).HasValue &&
                                (fallbackAccount = Setting.Accounts.Where(i => i.Id == fallbackId.Value).FirstOrDefault()) != null &&
                                (_originalAuthInfo == null || _originalAuthInfo.Id != fallbackAccount.Id))
                            {
                                // Post limit, go fallback
                                ShowToast("@" + this.AuthInfo.UnreliableScreenName + " is over daily status update limit.",
                                    "FALLBACK");
                                if (this._originalAuthInfo != null)
                                    this._originalAuthInfo = AuthInfo;
                                this.AuthInfo = fallbackAccount;
                                Twittaholic.UnlockStatic();
                                OperationQueueRunner.Dispatch(this);
                                return new Unit();
                            }
                            else
                            {
                                ShowToast(s, "TWEET ERROR");
                                throw ex;
                            }
                        });
                    */
                    return Observable.Throw<TwitterStatus>(ex);
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
                // TODO: Impl
                return AuthInfo.UpdateWithMedia(this.Status,
                    this.AttachedImageBin, "twitter_picture", false,
                    reply, geo_lat, geo_long);
                // , sendInBackground: true);
            }
            else
            {
                return AuthInfo.Update(this.Status, reply, geo_lat, geo_long);
            }
        }
    }
}