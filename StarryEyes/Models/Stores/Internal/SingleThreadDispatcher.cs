using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace StarryEyes.Models.Stores.Internal
{
    public class SingleThreadDispatcher<T> : IDisposable
    {
        private readonly Action<T> _action;
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly ManualResetEvent _event = new ManualResetEvent(false);
        private volatile bool _disposed;
        private readonly Task _task;

        public SingleThreadDispatcher(Action<T> action)
        {
            _task = Task.Factory.StartNew(WorkThread, TaskCreationOptions.LongRunning);
            _action = action;
        }

        public void Send(T item)
        {
            _queue.Enqueue(item);
            _event.Set();
        }

        private void WorkThread()
        {
            T item;
            while (!_disposed)
            {
                _event.Reset();
                while (_queue.TryDequeue(out item))
                {
                    _action(item);
                }
                _event.WaitOne();
            }
            _event.Dispose();
        }

        public void Dispose()
        {
            _disposed = true;
            _event.Set();
            GC.SuppressFinalize(this);
        }
    }
}
