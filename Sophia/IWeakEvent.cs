using System;

namespace Sophia
{
    /// <summary>
    /// Interface of weak event listening.
    /// </summary>
    /// <typeparam name="TEventArgs">type of event args</typeparam>
    public interface IWeakEvent<TEventArgs> where TEventArgs : EventArgs
    {
        IDisposable RegisterHandler(EventHandler<TEventArgs> handler);
    }
}