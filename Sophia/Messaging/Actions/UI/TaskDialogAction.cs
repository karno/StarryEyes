using System;
using System.Windows;
using Sophia.Messaging.UI;
using TaskDialogInterop;

namespace Sophia.Messaging.Actions.UI
{
    public class TaskDialogAction : MessageActionBase<TaskDialogMessage, FrameworkElement>
    {
        protected override void Invoke(TaskDialogMessage message)
        {
            try
            {
                var options = message.Options;
                options.Owner = Window.GetWindow(AssociatedObject);
                message.CompletionSource.SetResult(TaskDialog.Show(options));
            }
            catch (Exception)
            {
                var options = message.Options;
                message.CompletionSource.SetResult(TaskDialog.Show(options));
            }
        }
    }
}