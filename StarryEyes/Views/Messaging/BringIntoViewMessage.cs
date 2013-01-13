using Livet.Messaging;

namespace StarryEyes.Views.Messaging
{
    public class BringIntoViewMessage : InteractionMessage
    {
        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new BringIntoViewMessage();
        }
    }
}
