using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Xml.Linq;
using StarryEyes.Moon.Api;
using StarryEyes.Moon.Api.Parsing;
using StarryEyes.Moon.Api.Rest;
using StarryEyes.Moon.DataModel;
using StarryEyes.Moon.Net;
using StarryEyes.Moon.Util;

namespace StarryEyes.Moon.Imaging
{
    public class YFrogUploader : ImageUploaderBase
    {
        const string ApplicationKey = "238DGHOTa6a8f8356246fb3d3e9c7dae65cb3970";
        const string ApiEndpointUriString = "http://yfrog.com/api/xauth_upload";

        public override IObservable<TwitterStatus> Upload(Authorize.AuthenticateInfo authInfo, string status,
            byte[] attachedImageBin, long? in_reply_to_status_id = null,
            double? geo_lat = null, double? geo_long = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"key", ApplicationKey},
            }.Parametalize();
            return new MultipartableOAuthClient(ApiEndpoint.DefaultConsumerKey, ApiEndpoint.DefaultConsumerSecret, authInfo.AccessToken)
            {
                Url = ApiEndpointUriString,
            }
            .GetResponse(param.Select(p => new UploadContent(p.Key, p.Value))
                .Append(UploadContent.FromBinary("media", "attach.image", attachedImageBin)))
            .ReadString()
            .Select(s =>
            {
                using (var reader = new StringReader(s))
                {
                    var doc = XDocument.Load(reader);
                    return doc.Element("rsp").Element("mediaurl").ParseString();
                }
            })
            .SelectMany(s => authInfo.Update(status + " " + s, in_reply_to_status_id, geo_lat, geo_long));
        }
    }
}
