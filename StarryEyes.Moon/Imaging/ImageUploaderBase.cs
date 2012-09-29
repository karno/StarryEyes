using System;
using StarryEyes.Moon.Api.Rest;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.Moon.Imaging
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

        public virtual int UrlLengthPerImages
        {
            get { return 20; } // if HTTPS, this param is 21.
        }
    }
}

