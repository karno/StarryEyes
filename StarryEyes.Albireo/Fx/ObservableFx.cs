using System.Reactive.Concurrency;

// ReSharper disable CheckNamespace
namespace System.Reactive.Linq
// ReSharper restore CheckNamespace
{
    public static class ObservableFx
    {
        public static IObservable<T> Retry<T>(this IObservable<T> source, int retryCount, TimeSpan delaySpan)
        {
            return source.Retry<T, Exception>(retryCount, null, delaySpan, TaskPoolScheduler.Default);
        }

        public static IObservable<T> Retry<T, TException>(this IObservable<T> source, int retryCount,
            Action<TException> exAction, TimeSpan delaySpan, IScheduler scheduler)
            where TException : Exception
        {
            return source.Catch((TException ex) =>
            {
                if (exAction != null)
                {
                    exAction(ex);
                }

                //リトライ回数0回の場合はリトライしない
                if (retryCount == 0)
                {
                    return Observable.Throw<T>(ex);
                }

                //リトライ回数0未満)の場合無限リトライ
                if (retryCount < 0)
                {
                    return
                        Observable.Timer(delaySpan, scheduler)
                                  .SelectMany(_ => source.Retry(retryCount, exAction, delaySpan, scheduler));
                }

                return
                    Observable.Timer(delaySpan, scheduler)
                              .SelectMany(_ => source.Retry(retryCount, exAction, delaySpan, scheduler, 0));
            });
        }

        private static IObservable<T> Retry<T, TException>(this IObservable<T> source, int retryCount,
            Action<TException> exAction, TimeSpan delaySpan, IScheduler scheduler, int nowRetryCount)
            where TException : Exception
        {
            return source.Catch((TException ex) =>
            {
                nowRetryCount++;

                if (exAction != null)
                {
                    exAction(ex);
                }

                if (nowRetryCount < retryCount)
                {
                    return
                        Observable.Timer(delaySpan, scheduler)
                                  .SelectMany(
                                      _ => source.Retry(retryCount, exAction, delaySpan, scheduler, nowRetryCount));
                }

                return Observable.Throw<T>(ex);
            });
        }
    }
}
