using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Cursoring
    {
        public static IObservable<T> RetrieveAllCursor<T>(
            this IOAuthCredential credential,
            Func<IOAuthCredential, long, Task<ICursorResult<IEnumerable<T>>>> reader)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (reader == null) throw new ArgumentNullException("reader");
            long cursor = 0;
            var subject = new Subject<T>();
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var cr = await reader(credential, cursor);
                        cr.Result.ForEach(subject.OnNext);
                        if (!cr.CanReadNext)
                        {
                            break;
                        }
                        cursor = cr.NextCursor;
                    }
                    subject.OnCompleted();
                }
                catch (Exception ex)
                {
                    subject.OnError(ex);
                }
            });
            return subject;
        }
    }
}
