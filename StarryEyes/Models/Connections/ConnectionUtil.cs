using System;
using System.Reactive.Linq;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Hubs;

namespace StarryEyes.Models.Connections
{
    internal static class ConnectionUtil
    {
        public static void RegisterToStore(this IObservable<TwitterStatus> observable)
        {
            observable
                .SelectMany(StoreHub.MergeStore)
                .Subscribe(_ => { },
                ex => System.Diagnostics.Debug.WriteLine(ex));
        }
    }
}