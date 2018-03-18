using System.Windows.Controls;
using Livet.Behaviors.Messaging;

namespace StarryEyes.Views.Messaging.Behaviors
{
    public class TextBoxSetCaretInteractionMessageAction : InteractionMessageAction<TextBox>
    {
        protected override void InvokeAction(Livet.Messaging.InteractionMessage message)
        {
            var tcm = message as TextBoxSetCaretMessage;
            if (tcm == null) return;
            AssociatedObject.CaretIndex = tcm.CaretIndex;
            AssociatedObject.SelectionLength = tcm.SelectionLength;
        }
    }
}