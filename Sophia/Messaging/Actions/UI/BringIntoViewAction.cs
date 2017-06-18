using System.Windows;
using Sophia.Messaging.UI;

namespace Sophia.Messaging.Actions.UI
{
    public class BringIntoViewAction : MessageActionBase<BringIntoViewMessage, FrameworkElement>
    {
        protected override void Invoke(BringIntoViewMessage message)
        {
            AssociatedObject.BringIntoView();
        }
    }
}