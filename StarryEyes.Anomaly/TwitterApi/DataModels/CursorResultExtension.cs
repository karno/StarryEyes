using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public static class CursorResultExtension
    {
        public static IObservable<T> RetrieveAllCursor<T>(
            this IOAuthCredential credential,
            Func<IOAuthCredential, long, Task<ICursorResult<IEnumerable<T>>>> reader)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (reader == null) throw new ArgumentNullException("reader");
            long cursor = -1;
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

        public static IObservable<T> RetrieveAllCursor<T>(
            this IOAuthCredential credential, CancellationToken cancellationToken,
            Func<IOAuthCredential, long, CancellationToken, Task<ICursorResult<IEnumerable<T>>>> reader)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (reader == null) throw new ArgumentNullException("reader");
            long cursor = -1;
            var subject = new Subject<T>();
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var cr = await reader(credential, cursor, cancellationToken);
                        cr.Result.ForEach(subject.OnNext);
                        if (!cr.CanReadNext)
                        {
                            break;
                        }
                        cursor = cr.NextCursor;
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    subject.OnCompleted();
                }
                catch (Exception ex)
                {
                    subject.OnError(ex);
                }
            }, cancellationToken);
            return subject;
        }
    }
}
