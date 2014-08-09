using Livet;
using Livet.Messaging;

namespace StarryEyes.Views.Messaging
{
    public class BringIntoViewMessage : InteractionMessage
    {
        public BringIntoViewMessage()
        {
            DispatcherHelper.UIDispatcher.VerifyAccess();
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new BringIntoViewMessage();
        }
    }
}
