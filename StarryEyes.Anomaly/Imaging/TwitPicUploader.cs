using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Xml.Linq;
using StarryEyes.Breezy.Api;
using StarryEyes.Breezy.Api.Parsing;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Net;
using StarryEyes.Breezy.Util;

namespace StarryEyes.Breezy.Imaging
{
    public class TwitPicUploader : ImageUploaderBase
    {
        const string ApplicationKey = "f4e98ee376dc3e692342b6add361608d";
        const string ApiEndpointUriString = "http://api.twitpic.com/2/upload.xml";

        public override IObservable<TwitterStatus> Upload(Authorize.AuthenticateInfo authInfo, string status,
            byte[] attachedImageBin, long? in_reply_to_status_id = null,
            double? geo_lat = null, double? geo_long = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"key", ApplicationKey},
                {"message", status},
            }.Parametalize();
            return new MultipartableOAuthClient(ApiEndpoint.DefaultConsumerKey, ApiEndpoint.DefaultConsumerSecret,
                authInfo.AccessToken)
            {
                Url = ApiEndpointUriString,
            }
            .AsOAuthEcho()
            .GetResponse(param.Select(p => new UploadContent(p.Key, p.Value))
                .Append(UploadContent.FromBinary("media", "attach.png", attachedImageBin)))
            .ReadString()
            .Select(s =>
            {
                using (var reader = new StringReader(s))
                {
                    var doc = XDocument.Load(reader);
                    return doc.Element("image").Element("url").ParseString();
                }
            })
            .SelectMany(s => authInfo.Update(status + " " + s, in_reply_to_status_id, geo_lat, geo_long));
        }
    }
}

