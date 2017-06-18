using System.Windows.Controls;
using Sophia.Messaging.UI;

namespace Sophia.Messaging.Actions.UI
{
    public class TextBoxCaretAction : MessageActionBase<TextBoxCaretMessage, TextBox>
    {
        protected override void Invoke(TextBoxCaretMessage message)
        {
            var textBox = AssociatedObject;
            if (textBox == null) return;
            try
            {
                if (message.SelectionStart != null)
                {
                    textBox.SelectionStart = message.SelectionStart.Value;
                }
                if (message.SelectionLength != null)
                {
                    textBox.SelectionLength = message.SelectionLength.Value;
                }
                if (message.SelectionReplacingText != null)
                {
                    textBox.SelectedText = message.SelectionReplacingText;
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}