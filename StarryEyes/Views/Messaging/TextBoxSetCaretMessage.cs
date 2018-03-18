using Livet;
using Livet.Messaging;

namespace StarryEyes.Views.Messaging
{
    public class TextBoxSetCaretMessage : InteractionMessage
    {
        public int CaretIndex { get; }

        public int SelectionLength { get; }

        public TextBoxSetCaretMessage(string messageKey) : base(messageKey)
        {
        }

        public TextBoxSetCaretMessage(int caretIndex, int selectionLength)
        {
            DispatcherHelper.UIDispatcher.VerifyAccess();
            CaretIndex = caretIndex;
            SelectionLength = selectionLength;
        }

        public TextBoxSetCaretMessage(string messageKey, int caretIndex, int selectionLength)
            : base(messageKey)
        {
            CaretIndex = caretIndex;
            SelectionLength = selectionLength;
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new TextBoxSetCaretMessage(MessageKey, CaretIndex, SelectionLength);
        }
    }
}