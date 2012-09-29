using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using Codeplex.OAuth;

namespace StarryEyes.Moon.Net
{
    public class MultipartableOAuthClient : OAuthBase
    {
        public AccessToken AccessToken { get; private set; }
        public string Url { get; set; }
        public MethodType MethodType { get { return Codeplex.OAuth.MethodType.Post; } } // static
        public IEnumerable<UploadContent> Paremeters { get; set; }
        public MultipartableOAuthClient(string ckey, string csec, AccessToken token)
            : base(ckey, csec)
        {
            if (token == null)
                throw new NullReferenceException("token");
            AccessToken = token;
        }


        private bool _isUseOAuthEcho = false;
        private string _serviceProvider = null;
        private string _realm = null;
        public MultipartableOAuthClient AsOAuthEcho(
            string serviceProvider = "https://api.twitter.com/1/account/verify_credentials.json",
            string realm = "http://api.twitter.com/")
        {
            if (_isUseOAuthEcho)
                throw new InvalidOperationException("OAuth Echo is already initialized.");
            this._serviceProvider = serviceProvider;
            this._realm = realm;
            _isUseOAuthEcho = true;
            return this;
        }

        private WebRequest CreateRequest()
        {
            var parameters = ConstructBasicParameters(Url, MethodType, AccessToken);
            var authHeader = BuildAuthorizationHeader(parameters);
            var req = (HttpWebRequest)WebRequest.Create(Url);
            req.Headers[HttpRequestHeader.Authorization] = authHeader;
            req.Method = MethodType.ToUpperString();
            return req;
        }

        public IObservable<WebResponse> GetResponse(IEnumerable<UploadContent> uploadContents)
        {
            if (Url == null)
                throw new InvalidOperationException("Url is not set.");
            if (this.MethodType != Codeplex.OAuth.MethodType.Post)
                throw new InvalidOperationException("GetResponse with uploading multipart content is must be POST request.");

            var req = this.CreateRequest();
            var boundary = Guid.NewGuid().ToString();
            req.ContentType = "multipart/form-data; boundary=" + boundary;
            boundary = "--" + boundary;

            return Observable.Defer(() =>
                Observable.FromAsyncPattern<Stream>(req.BeginGetRequestStream, req.EndGetRequestStream)())
                .Do(stream =>
                {
                    using (stream)
                    using (var sw = new StreamWriter(stream, new UTF8Encoding(false)))
                    {
                        uploadContents.ForEach(uc =>
                        {
                            sw.WriteLine(boundary);
                            sw.WriteLine("Content-Disposition: form-data; name=\"" + uc.Name + "\"" +
                                (String.IsNullOrEmpty(uc.FileName) ? "" : ("; filename=\"" + uc.FileName + "\"")));
                            if (uc.Headers != null)
                                uc.Headers.ForEach(header => sw.WriteLine(header.Key + ": " + header.Value));
                            sw.WriteLine(); // add empty line
                            sw.Flush();
                            uc.Writer(stream, sw);
                            stream.Flush();
                        });
                        sw.WriteLine(boundary + "--");
                        sw.Flush();
                    }
                })
                .SelectMany(_ => Observable.FromAsyncPattern<WebResponse>(req.BeginGetResponse, req.EndGetResponse)());
        }
    }

    public class UploadContent
    {
        public static UploadContent FromBinary(string name, string dummyFileName, byte[] imageBinary)
        {
            return new UploadContent(name, dummyFileName,
                new Dictionary<string, string>()
                {
                    {"Content-Type", "application/octet/stream"},
                    {"Content-Transfer-Encoding", "binary"}
                }, (s, sw) =>
                {
                    sw.Flush();
                    s.Write(imageBinary, 0, imageBinary.Length);
                    s.Flush();
                    sw.WriteLine();
                });
        }

        public UploadContent(string name, string text)
        {
            this.Name = name;
            this.Writer = (_, sw) =>
            {
                sw.WriteLine(text);
            };
        }

        public UploadContent(string name, string fileName, Dictionary<string, string> otherHeaders,
            Action<Stream, StreamWriter> writer)
        {
            this.Name = name;
            this.FileName = fileName;
            this.Headers = otherHeaders;
            this.Writer = writer;
        }

        public string Name { get; set; }

        public string FileName { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public Action<Stream, StreamWriter> Writer { get; set; }
    }
}
