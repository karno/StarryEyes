using System.Threading;

// ReSharper disable CheckNamespace
namespace System.Reactive.Linq
// ReSharper restore CheckNamespace
{
    public static class ObservableDebugHelper
    {
        private static readonly ObservableDebugTracker _defaultTracker = new ObservableDebugTracker();

        public static ObservableDebugTracker DefaultTracker
        {
            get { return _defaultTracker; }
        }

        public static IObservable<T> Track<T>(this IObservable<T> source)
        {
            return Track(source, DefaultTracker);
        }

        public static IObservable<T> Track<T>(this IObservable<T> source, ObservableDebugTracker tracker)
        {
            var ics = Interlocked.Increment(ref tracker._subscriptionCount);
#if DEBUG
            System.Diagnostics.Debug.WriteLine("# initialized subscription: " + typeof(T).FullName + " current subscription: " + ics);
            return source.Finally(() =>
            {
                var dcs = Interlocked.Decrement(ref tracker._subscriptionCount);
                System.Diagnostics.Debug.WriteLine("* finalized subscription: " + typeof(T).FullName + " current subscription: " + dcs);
            });
#else
            Interlocked.Increment(ref tracker._subscriptionCount);
            return source.Finally(() => Interlocked.Decrement(ref tracker._subscriptionCount));
#endif
        }
    }

    public class ObservableDebugTracker
    {
        internal int _subscriptionCount;

        public int SubscriptionCount
        {
            get { return _subscriptionCount; }
        }
    }
}
