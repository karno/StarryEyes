using System.Windows.Controls;
using Livet.Behaviors.Messaging;
using Livet.Messaging;
using StarryEyes.Views.Utils;

namespace StarryEyes.Views.Messaging.Behaviors
{
    public class ScrollIntoViewInteractionMessageAction : InteractionMessageAction<ItemsControl>
    {
        protected override void InvokeAction(InteractionMessage message)
        {
            var m = message as ScrollIntoViewMessage;
            if (m != null)
            {
                var vsp = AssociatedObject.FindVisualChild<VirtualizingStackPanel>();
                vsp?.BringIndexIntoViewPublic(m.Index);
            }
        }
    }
}