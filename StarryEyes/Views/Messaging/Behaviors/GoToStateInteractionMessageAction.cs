using System;
using System.Windows;
using Livet.Behaviors.Messaging;
using Microsoft.Expression.Interactivity;

namespace StarryEyes.Views.Messaging.Behaviors
{
    public class GoToStateInteractionMessageAction : InteractionMessageAction<FrameworkElement>
    {
        protected override void InvokeAction(Livet.Messaging.InteractionMessage message)
        {
            var gtsm = message as GoToStateMessage;
            if (gtsm == null) return;
            try
            {
                gtsm.Response = VisualStateUtilities.GoToState(
                    this.AssociatedObject, gtsm.StateName, gtsm.UseTransitions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
    }
}
