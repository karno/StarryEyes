using Livet.Messaging;

namespace StarryEyes.Views.Messaging
{
    public class BringIntoViewMessage : InteractionMessage
    {
        public BringIntoViewMessage()
        {
            DispatcherHolder.VerifyAccess();
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new BringIntoViewMessage();
        }
    }
}
