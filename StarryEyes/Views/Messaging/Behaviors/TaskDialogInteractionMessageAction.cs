using System.Windows;
using Livet.Behaviors.Messaging;
using Livet.Messaging;
using TaskDialogInterop;

namespace StarryEyes.Views.Messaging.Behaviors
{
    public class TaskDialogInteractionMessageAction : InteractionMessageAction<FrameworkElement>
    {
        protected override void InvokeAction(InteractionMessage message)
        {
            var tdm = message as TaskDialogMessage;
            if (tdm == null) return;

            var options = tdm.Options;
            options.Owner = Window.GetWindow(this.AssociatedObject);
            var result = TaskDialog.Show(options);
            if (tdm.ResultHandler != null)
                tdm.ResultHandler(result);
        }
    }
}
