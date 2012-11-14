using System;
using StarryEyes.Models.Stores;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Connections
{
    internal static class ConnectionUtil
    {
        public static void RegisterToStore(this IObservable<TwitterStatus> observable)
        {
            observable
                .Subscribe(_ => StatusStore.Store(_), ex => System.Diagnostics.Debug.WriteLine(ex));
        }
    }
}