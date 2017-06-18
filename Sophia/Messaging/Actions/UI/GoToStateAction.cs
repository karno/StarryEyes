using System;
using System.Windows;
using Sophia.Messaging.UI;

namespace Sophia.Messaging.Actions.UI
{
    public class GoToStateAction : MessageActionBase<GoToStateMessage, FrameworkElement>
    {
        protected override void Invoke(GoToStateMessage message)
        {
            try
            {
                message.CompletionSource.TrySetResult(
                    VisualStateManager.GoToState(AssociatedObject, message.StateName, message.UseTransitions));
            }
            catch (Exception ex)
            {
                message.CompletionSource.TrySetException(ex);
            }
        }
    }
}