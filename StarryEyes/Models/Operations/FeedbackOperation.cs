using System;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
            return req.GetRequestStreamAsync().ToObservable()
                .SelectMany(s =>
                {
                    s.Write(pdb, 0, pdb.Length);
                    s.Close();
                    return req.GetResponseAsync().ToObservable();
                })
                .Select(_ => new Unit());
        }
    }
}
