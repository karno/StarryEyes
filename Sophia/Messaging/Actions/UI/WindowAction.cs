using System.Windows;
using Sophia.Messaging.UI;

namespace Sophia.Messaging.Actions.UI
{
    public class WindowAction : MessageActionBase<WindowMessage, Window>
    {
        protected override void Invoke(WindowMessage message)
        {
            if (message.IsClose)
            {
                AssociatedObject.Close();
            }
            else
            {
                message.WindowInfo?.Apply(AssociatedObject);
            }
        }
    }
}