using System;
using System.Reactive.Linq;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Connections
{
    internal static class ConnectionUtil
    {
        public static void RegisterToStore(this IObservable<TwitterStatus> observable)
        {
            observable
                .SelectMany(StoreHelper.MergeStore)
                .Subscribe(_ => { },
                ex => System.Diagnostics.Debug.WriteLine(ex));
        }
    }
}