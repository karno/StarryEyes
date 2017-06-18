using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sophia._Internals;

namespace Sophia.Messaging.Core
{
    public class Messenger : IDisposable
    {
        private bool _disposed;

        private readonly WeakEvent<MessageEventArgs> _messageRaiseEvent;

        [NotNull]
        public IWeakEvent<MessageEventArgs> MessageRaiseEvent => _messageRaiseEvent;

        public Messenger()
        {
            _messageRaiseEvent = new WeakEvent<MessageEventArgs>();
        }

        public async Task<T> RaiseAsync<T>([NotNull] T message) where T : MessageBase
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (_disposed) throw new ObjectDisposedException(nameof(Messenger));
            var args = new MessageEventArgs(message);
            _messageRaiseEvent.Invoke(this, args);
            return (T)await args.CompletionSource.Task.ConfigureAwait(false);
        }

        public async Task<TResponse> GetResponseAsync<TResponse>(
            [NotNull] ResponsiveMessageBase<TResponse> message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (_disposed) throw new ObjectDisposedException(nameof(Messenger));
            var responsive = await RaiseAsync(message);
            return await responsive.CompletionSource.Task.ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Messenger()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _messageRaiseEvent.Dispose();
            }
            _disposed = true;
        }
    }
}