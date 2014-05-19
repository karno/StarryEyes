using System;
using System.Threading.Tasks;
using Livet.Messaging;

// ReSharper disable once CheckNamespace
namespace StarryEyes
{
    public static class MessengerHotfix
    {
        public static void RaiseSafe(this InteractionMessenger messenger,
            Func<InteractionMessage> messageFactory)
        {
            DispatcherHolder.Enqueue(() => messenger.Raise(messageFactory()));
        }

        public static void RaiseSafeSync(this InteractionMessenger messenger,
            Func<InteractionMessage> messageFactory)
        {
            DispatcherHolder.Invoke(() => messenger.Raise(messageFactory()));
        }

        public static async Task RaiseSafeAsync(this InteractionMessenger messenger,
            Func<InteractionMessage> messageFactory)
        {
            await DispatcherHolder.BeginInvoke(() => messenger.Raise(messageFactory()));
        }

        public static T GetResponseSafe<T>(this InteractionMessenger messenger,
                    Func<T> messageFactory) where T : ResponsiveInteractionMessage
        {
            return DispatcherHolder.Invoke(() => messenger.GetResponse(messageFactory()));
        }
    }
}
