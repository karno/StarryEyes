using System;
using System.Threading.Tasks;
using Livet;
using Livet.Messaging;

// ReSharper disable once CheckNamespace
namespace StarryEyes
{
    public static class MessengerHotfix
    {
        public static void RaiseSafe(this InteractionMessenger messenger,
            Func<InteractionMessage> messageFactory)
        {
            DispatcherHelper.UIDispatcher.InvokeAsync(() => messenger.Raise(messageFactory()));
        }

        public static void RaiseSafeSync(this InteractionMessenger messenger,
            Func<InteractionMessage> messageFactory)
        {
            if (DispatcherHelper.UIDispatcher.CheckAccess())
            {
                messenger.Raise(messageFactory());
            }
            else
            {
                DispatcherHelper.UIDispatcher.Invoke(() => messenger.Raise(messageFactory()));
            }
        }

        public static async Task RaiseSafeAsync(this InteractionMessenger messenger,
            Func<InteractionMessage> messageFactory)
        {
            await DispatcherHelper.UIDispatcher.InvokeAsync(() => messenger.Raise(messageFactory()));
        }

        public static T GetResponseSafe<T>(this InteractionMessenger messenger,
                    Func<T> messageFactory) where T : ResponsiveInteractionMessage
        {
            return DispatcherHelper.UIDispatcher.CheckAccess()
                ? messenger.GetResponse(messageFactory())
                : DispatcherHelper.UIDispatcher.Invoke(() => messenger.GetResponse(messageFactory()));
        }
    }
}
