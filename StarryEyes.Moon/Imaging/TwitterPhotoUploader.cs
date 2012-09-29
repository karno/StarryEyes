using System;
using StarryEyes.Moon.Api.Rest;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.Moon.Imaging
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
