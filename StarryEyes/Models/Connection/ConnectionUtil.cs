using System;
using StarryEyes.Models.Store;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.Models.Connection
{
    internal static class ConnectionUtil
    {
        public static void RegisterToStore(this IObservable<TwitterStatus> observable)
        {
            observable.Subscribe(_ => StatusStore.Store(_), ex => System.Diagnostics.Debug.WriteLine(ex));
        }
    }
}