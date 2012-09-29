using System;
using System.IO;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace StarryEyes.Models.Operations
{
    public class FeedbackOperation : OperationBase<Unit>
    {
        const string FeedbackUri = "http://krile.starwing.net/report.php";

        public FeedbackOperation() { }

        public FeedbackOperation(string report)
        {
            this._report = report;
        }

        private string _report;
        public string Report
        {
            get { return _report; }
            set { _report = value; }
        }

        protected override IObservable<Unit> RunCore()
        {
            string postData = "error=" + Uri.EscapeDataString(Report);
            byte[] pdb = Encoding.UTF8.GetBytes(postData);
            var req = WebRequest.Create(FeedbackUri);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            return Observable.FromAsyncPattern<Stream>(req.BeginGetRequestStream, req.EndGetRequestStream)()
                .SelectMany(s =>
                {
                    s.Write(pdb, 0, pdb.Length);
                    s.Close();
                    return Observable.FromAsyncPattern<WebResponse>(req.BeginGetResponse, req.EndGetResponse)();
                })
                .Select(_ => new Unit());
        }
    }
}
