using System.Threading.Tasks;
using System.Windows;
using Livet.Behaviors.Messaging;
using Livet.Messaging;
using Livet.Messaging.IO;
using Microsoft.Win32;

// hotfixes for Livet.

namespace StarryEyes
{
    public static class LivetFixer
    {
        public static Task<T> GetResponseAsync<T>(this InteractionMessenger messenger, T message)
            where T : ResponsiveInteractionMessage
        {
            return messenger.GetResponseAsync(message, null);
        }
    }
}
