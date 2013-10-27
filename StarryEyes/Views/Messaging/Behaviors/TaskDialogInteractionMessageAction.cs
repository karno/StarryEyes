using System;
using System.Windows;
using Livet.Behaviors.Messaging;
using Livet.Messaging;
using StarryEyes.Nightmare.Windows;

namespace StarryEyes.Views.Messaging.Behaviors
{
    public class TaskDialogInteractionMessageAction : InteractionMessageAction<FrameworkElement>
    {
        protected override void InvokeAction(InteractionMessage message)
        {
            var tdm = message as TaskDialogMessage;
            if (tdm == null) return;

            try
            {
                var options = tdm.Options;
                options.Owner = Window.GetWindow(this.AssociatedObject);
                tdm.Response = TaskDialog.Show(options);
            }
            catch (InvalidOperationException)
            {
                var options = tdm.Options;
                tdm.Response = TaskDialog.Show(options);
            }
        }
    }
}
