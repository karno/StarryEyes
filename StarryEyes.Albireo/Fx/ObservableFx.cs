using System.Collections.Generic;
using System.Reactive.Concurrency;

namespace System.Reactive.Linq
{
    public static class ObservableFx
    {
        public static IObservable<T> Retry<T>(this IObservable<T> source, int retryCount, TimeSpan delaySpan)
        {
            return source.Retry<T, Exception>(retryCount, null, delaySpan, Scheduler.ThreadPool);
        }

        public static IObservable<T> Retry<T, TException>(this IObservable<T> source, int retryCount, Action<TException> exAction, TimeSpan delaySpan)
            where TException : Exception
        {
            return source.Retry(retryCount, exAction, delaySpan, Scheduler.ThreadPool);
        }

        public static IObservable<T> Retry<T, TException>(this IObservable<T> source, int retryCount, Action<TException> exAction, TimeSpan delaySpan, IScheduler scheduler)
            where TException : Exception
        {
            return source.Catch((TException ex) =>
            {
                if (exAction != null)
                {
                    exAction(ex);
                }

                //リトライ回数1回の場合はリトライしない
                if (retryCount == 1)
                {
                    return Observable.Throw<T>(ex);
                }

                //リトライ回数0(一応0未満)の場合無限リトライ
                if (retryCount <= 0)
                {
                    return Observable.Timer(delaySpan, scheduler).SelectMany(_ => source.Retry(retryCount, exAction, delaySpan, scheduler));
                }

                int nowRetryCount = 1;

                return Observable.Timer(delaySpan, scheduler).SelectMany(_ => source.Retry(retryCount, exAction, delaySpan, scheduler, nowRetryCount));
            });
        }

        private static IObservable<T> Retry<T, TException>(this IObservable<T> source, int retryCount, Action<TException> exAction, TimeSpan delaySpan, IScheduler scheduler, int nowRetryCount)
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
                    return Observable.Timer(delaySpan, scheduler).SelectMany(_ => source.Retry(retryCount, exAction, delaySpan, scheduler, nowRetryCount));
                }

                return Observable.Throw<T>(ex);
            });
        }

        public static IObservable<T> While<T>(this IObservable<T> source, Func<bool> condition)
        {
            return WhileCore(condition, source).Concat();
        }

        public static IObservable<T> DoWhile<T>(this IObservable<T> source, Func<bool> condition)
        {
            return source.Concat(source.While(condition));
        }

        private static IEnumerable<IObservable<T>> WhileCore<T>(Func<bool> condition, IObservable<T> source)
        {
            while (condition())
            {
                yield return source;
            }
        }

    }
}
