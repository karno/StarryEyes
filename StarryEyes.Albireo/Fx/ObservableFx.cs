using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;

// ReSharper disable CheckNamespace
namespace System.Reactive.Linq
// ReSharper restore CheckNamespace
{
    public static class ObservableFx
    {
        public static IObservable<NotifyCollectionChangedEventArgs> ListenCollectionChanged(
            this INotifyCollectionChanged collection)
        {
            return Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>
                (h => collection.CollectionChanged += h,
                 h => collection.CollectionChanged -= h)
                             .Select(p => p.EventArgs);
        }

        public static IObservable<PropertyChangedEventArgs> ListenPropertyChanged(
            this INotifyPropertyChanged listenee)
        {
            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>
                (h => listenee.PropertyChanged += h,
                 h => listenee.PropertyChanged -= h)
                             .Select(p => p.EventArgs);
        }

        public static IObservable<PropertyChangedEventArgs> ListenPropertyChanged<T>(
            this INotifyPropertyChanged listenee, Expression<Func<T>> propertyExpression)
        {
            String propname = ExtractPropertyName(propertyExpression);
            if (propname == null)
                throw new ArgumentException("Unknown property name.");
            return ListenPropertyChanged(listenee)
                .Where(p => p.PropertyName == propname);
        }

        public static IObservable<PropertyChangingEventArgs> ListenPropertyChanging(
            this INotifyPropertyChanging listenee)
        {
            return Observable.FromEventPattern<PropertyChangingEventHandler, PropertyChangingEventArgs>
                (h => listenee.PropertyChanging += h,
                 h => listenee.PropertyChanging -= h)
                             .Select(p => p.EventArgs);
        }

        public static IObservable<PropertyChangingEventArgs> ListenPropertyChanging<T>(
            this INotifyPropertyChanging listenee, Expression<Func<T>> propertyExpression)
        {
            String propname = ExtractPropertyName(propertyExpression);
            if (propname == null)
                throw new ArgumentException("Unknown property name.");
            return ListenPropertyChanging(listenee)
                .Where(p => p.PropertyName == propname);
        }

        private static string ExtractPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            var memberExpression = propertyExpression.Body as MemberExpression;
            return memberExpression != null ? memberExpression.Member.Name : null;
        }

        public static IObservable<T> Retry<T>(this IObservable<T> source, int retryCount, TimeSpan delaySpan)
        {
            return source.Retry<T, Exception>(retryCount, null, delaySpan, TaskPoolScheduler.Default);
        }

        public static IObservable<T> Retry<T, TException>(this IObservable<T> source, int retryCount, Action<TException> exAction, TimeSpan delaySpan)
            where TException : Exception
        {
            return source.Retry(retryCount, exAction, delaySpan, TaskPoolScheduler.Default);
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

                return
                    Observable.Timer(delaySpan, scheduler)
                              .SelectMany(_ => source.Retry(retryCount, exAction, delaySpan, scheduler, 1));
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

        public static IObservable<T> ConcatIfEmpty<T>(this IObservable<T> source, Func<IObservable<T>> next)
        {
            return source
                .Materialize()
                .Select((n, i) => (n.Kind == NotificationKind.OnCompleted && i == 0) ?
                    next().Materialize() : Observable.Return(n))
                .SelectMany(ns => ns)
                .Dematerialize();
        }

        public static IObservable<T> MergeOrderBy<T, TKey>(this IEnumerable<IObservable<T>> orderedObservables,
                                                           Func<T, TKey> keySelector)
        {
            return orderedObservables.MergeOrderByCore(keySelector, false);
        }

        public static IObservable<T> MergeOrderByDescending<T, TKey>(this IEnumerable<IObservable<T>> orderedObservables,
                                                                   Func<T, TKey> keySelector)
        {
            return orderedObservables.MergeOrderByCore(keySelector, true);
        }

        private static IObservable<T> MergeOrderByCore<T, TKey>(this IEnumerable<IObservable<T>> orderedObservables,
                                                                   Func<T, TKey> keySelector, bool descending)
        {
            var queues = new List<CompletableQueue<T>>();
            var observablePair = orderedObservables
                .Select(observable => new { Queue = new CompletableQueue<T>(), Observable = observable })
                .Do(pair => queues.Add(pair.Queue))
                .ToArray();
            return observablePair
                .ToObservable()
                .SelectMany(set => set.Observable
                                      .Do(set.Queue.SynchronizedEnqueue)
                                      .Finally(() => set.Queue.IsCompleted = true))
                .Materialize()
                .SelectMany(n =>
                {
                    lock (queues)
                    {
                        var returns = new List<Notification<T>>();
                        // dequeue and check items
                        while (queues.All(q => q.IsCompleted || q.SynchorizedCount > 0) &&
                               queues.Any(q => q.SynchorizedCount > 0))
                        {
                            var dequeues = queues.Where(q => q.SynchorizedCount > 0)
                                                 .Select(q => q.SynchornizedDequeue());
                            var ordered = descending
                                              ? dequeues.OrderByDescending(keySelector)
                                              : dequeues.OrderBy(keySelector);
                            ordered
                                .Select(Notification.CreateOnNext)
                                .ForEach(returns.Add);
                        }
                        if (n.Kind == NotificationKind.OnCompleted)
                        {
                            // clean up all queues
                            returns.Add(Notification.CreateOnCompleted<T>());
                        }
                        return returns;
                    }
                })
                .Dematerialize();
        }

        private class CompletableQueue<T>
        {
            public bool IsCompleted { get; set; }

            private readonly Queue<T> _queue = new Queue<T>();
            public Queue<T> Queue
            {
                get { return _queue; }
            }

            public void SynchronizedEnqueue(T item)
            {
                lock (_queue)
                {
                    _queue.Enqueue(item);
                }
            }

            public int SynchorizedCount
            {
                get
                {
                    lock (_queue)
                    {
                        return _queue.Count;
                    }
                }
            }

            public T SynchornizedDequeue()
            {
                lock (_queue)
                {
                    return _queue.Dequeue();
                }
            }

            public override string ToString()
            {
                return "Queue: " + _queue + " , Completed: " + IsCompleted;
            }
        }

        public static IObservable<T> OrderBy<T, TKey>(this IObservable<T> observable,
            Func<T, TKey> keySelector)
        {
            var material = new List<Notification<T>>();
            return observable.Materialize()
                .Select(_ =>
                {
                    switch (_.Kind)
                    {
                        case NotificationKind.OnError:
                            return EnumerableEx.Return(_);
                        case NotificationKind.OnNext:
                            material.Add(_);
                            break;
                        case NotificationKind.OnCompleted:
                            return material.OrderBy(i => keySelector(i.Value))
                                .Concat(EnumerableEx.Return(_));
                    }
                    return Enumerable.Empty<Notification<T>>();
                })
                .SelectMany(_ => _)
                .Dematerialize();
        }

        public static IObservable<T> OrderByDescending<T, TKey>(this IObservable<T> observable,
            Func<T, TKey> keySelector)
        {
            var material = new List<Notification<T>>();
            return observable.Materialize()
                .Select(_ =>
                {
                    switch (_.Kind)
                    {
                        case NotificationKind.OnError:
                            return EnumerableEx.Return(_);
                        case NotificationKind.OnNext:
                            material.Add(_);
                            break;
                        case NotificationKind.OnCompleted:
                            return material.OrderByDescending(i => keySelector(i.Value))
                                .Concat(EnumerableEx.Return(_));
                    }
                    return Enumerable.Empty<Notification<T>>();
                })
                .SelectMany(_ => _)
                .Dematerialize();
        }
    }
}
