using System.Windows;
using Livet.Behaviors.Messaging;

namespace StarryEyes.Views.Messaging.Behaviors
{
    public class BringIntoViewInteractionMessageAction : InteractionMessageAction<FrameworkElement>
    {
        protected override void InvokeAction(Livet.Messaging.InteractionMessage message)
        {
            if (message is BringIntoViewMessage)
            {
                this.AssociatedObject.BringIntoView();
            }
        }
    }
}
