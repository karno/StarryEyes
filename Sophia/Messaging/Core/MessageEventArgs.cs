using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Sophia.Messaging.Core
{
    public sealed class MessageEventArgs : EventArgs
    {
        [NotNull]
        public TaskCompletionSource<MessageBase> CompletionSource { get; }

        [NotNull]
        public MessageBase Message { get; }

        public MessageEventArgs([NotNull] MessageBase message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Message = message;
            CompletionSource = new TaskCompletionSource<MessageBase>();
        }
    }
}