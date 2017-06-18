using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace Sophia._Internals
{
    /// <summary>
    /// Implementation class of IWeakEvent.
    /// </summary>
    /// <typeparam name="TEventArgs">type of event args</typeparam>
    internal sealed class WeakEvent<TEventArgs> : IWeakEvent<TEventArgs>, IDisposable where TEventArgs : EventArgs
    {
        private bool _disposed;

        private long _listenId;

        private readonly Dictionary<long, WeakReference<EventHandler<TEventArgs>>> _handlerDictionary;

        public WeakEvent()
        {
            _handlerDictionary = new Dictionary<long, WeakReference<EventHandler<TEventArgs>>>();
        }

        public IDisposable RegisterHandler([NotNull] EventHandler<TEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_disposed) throw new ObjectDisposedException(nameof(WeakEvent<TEventArgs>));
            var id = Interlocked.Increment(ref _listenId);
            lock (_handlerDictionary)
            {
                _handlerDictionary.Add(id, new WeakReference<EventHandler<TEventArgs>>(handler));
            }
            return new WeakEventUnsubscriber(this, id);
        }

        public void Invoke(object caller, TEventArgs eventArgs)
        {
            EventHandler<TEventArgs>[] listeners;
            lock (_handlerDictionary)
            {
                listeners = GetLiveHandlers().ToArray();
            }
            foreach (var handler in listeners)
            {
                handler(caller, eventArgs);
            }
        }

        private IEnumerable<EventHandler<TEventArgs>> GetLiveHandlers()
        {
            // freeze keys to prevent changes during enumeration
            var keys = _handlerDictionary.Keys.ToArray();
            foreach (var key in keys)
            {
                var value = _handlerDictionary[key];
                EventHandler<TEventArgs> handler;
                if (value.TryGetTarget(out handler))
                {
                    yield return handler;
                }
                else
                {
                    // remove handler to prevent next call
                    _handlerDictionary.Remove(key);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            lock (_handlerDictionary)
            {
                _handlerDictionary.Clear();
            }
            _disposed = true;
        }

        private sealed class WeakEventUnsubscriber : IDisposable
        {
            private readonly WeakEvent<TEventArgs> _parent;
            private readonly long _id;

            internal WeakEventUnsubscriber([NotNull] WeakEvent<TEventArgs> parent, long id)
            {
                if (parent == null) throw new ArgumentNullException(nameof(parent));
                _parent = parent;
                _id = id;
            }

            public void Dispose()
            {
                lock (_parent._handlerDictionary)
                {
                    _parent._handlerDictionary.Remove(_id);
                }
            }
        }
    }
}