using System.Windows;
using Sophia.Messaging.UI;

namespace Sophia.Messaging.Actions.UI
{
    public class OpenWindowAction : MessageActionBase<OpenWindowMessage, FrameworkElement>
    {
        protected override void Invoke(OpenWindowMessage message)
        {
            var window = message.CreateWindowInstance();
            if (message.Owner.SetParent)
            {
                if (message.Owner.ExplicitParent != null)
                {
                    window.Owner = message.Owner.ExplicitParent;
                }
                else
                {
                    var callerWindow = Window.GetWindow(AssociatedObject);
                    window.Owner = callerWindow;
                }
            }
            if (message.ShowAsDialog)
            {
                window.ShowDialog();
            }
            else
            {
                window.Show();
            }
        }
    }
}