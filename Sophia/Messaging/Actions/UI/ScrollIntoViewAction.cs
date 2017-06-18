using System.Windows.Controls;
using Sophia.Messaging.UI;
using Sophia.Utilities;

namespace Sophia.Messaging.Actions.UI
{
    public class ScrollIntoViewAction : MessageActionBase<ScrollIntoViewMessage, ItemsControl>
    {
        protected override void Invoke(ScrollIntoViewMessage message)
        {
            var vsp = AssociatedObject.FindVisualChild<VirtualizingStackPanel>();
            vsp?.BringIndexIntoViewPublic(message.Index);
        }
    }
}