using System;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Models.Connection
{
    internal static class ConnectionUtil
    {
        public static void RegisterToStore(this IObservable<TwitterStatus> observable)
        {
            observable.Subscribe(_ => StatusStore.Store(_), ex => System.Diagnostics.Debug.WriteLine(ex));
        }
    }
}
