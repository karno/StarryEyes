using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Breezy.Imaging
{
    public class TwitterPhotoUploader : ImageUploaderBase
    {
        public override IObservable<TwitterStatus> Upload(AuthenticateInfo authInfo, string status,
            byte[] attachedImageBin, long? in_reply_to_status_id = null,
            double? geo_lat = null, double? geo_long = null)
        {
            return authInfo.UpdateWithMedia(
                status,
                attachedImageBin,
                "twitter_picture",
                false,
                in_reply_to_status_id,
                geo_lat, geo_long);
        }
    }
}
