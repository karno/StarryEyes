using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Breezy.Imaging
{
    public abstract class ImageUploaderBase
    {
        /// <summary>
        /// returns updated status.
        /// </summary>
        public abstract IObservable<TwitterStatus> Upload(AuthenticateInfo authInfo,
            string status, byte[] attachedImageBin, long? in_reply_to_status_id = null,
            double? geo_lat = null, double? geo_long = null);

        protected IObservable<TwitterStatus> Update(AuthenticateInfo info,
            string status, long? inReplyToId, long? geoLat, long? geoLong)
        {
            return info.Update(status, inReplyToId, geoLat, geoLong);
        }

        public virtual bool UseHttpsUrl
        {
            get { return false; }
        }
    }
}

