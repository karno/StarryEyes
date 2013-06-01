using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace StarryEyes.Anomaly.Utils
{
    public static class ReactiveUtility
    {
        public static IObservable<T> ToObservable<T>(this Task<IEnumerable<T>> asyncResult)
        {
            return Observable.FromAsync(() => asyncResult)
                      .SelectMany(s => s);
        }
    }
}
