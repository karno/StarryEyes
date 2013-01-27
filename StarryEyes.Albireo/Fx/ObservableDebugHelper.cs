using System;
using System.Reactive.Linq;
using System.Threading;

// ReSharper disable CheckNamespace
namespace System.Reactive.Linq
// ReSharper restore CheckNamespace
{
    public static class ObservableDebugHelper
    {
        private static int _subscription = 0;
        public static int CurrentAliveSubscriptions
        {
            get { return _subscription; }
        }

        public static IObservable<T> Track<T>(this IObservable<T> source)
        {
            int ics = Interlocked.Increment(ref _subscription);
#if DEBUG
            System.Diagnostics.Debug.WriteLine("# initialized subscription: " + typeof(T).FullName + " current subscription: " + ics);
            return source.Finally(() =>
            {
                var dcs = Interlocked.Decrement(ref _subscription);
                System.Diagnostics.Debug.WriteLine("* finalized subscription: " + typeof(T).FullName + " current subscription: " + dcs);
            });
#else
            Interlocked.Increment(ref _subscription);
            return source.Finally(() => Interlocked.Decrement(ref _subscription));
#endif
        }
    }
}
