using Livet.Messaging;

namespace StarryEyes.Views.Messaging
{
    public class TextBoxSetCaretMessage : InteractionMessage
    {
        public int CaretIndex { get; set; }

        public int SelectionLength { get; set; }

        public TextBoxSetCaretMessage(string messageKey) : base(messageKey) { }
        public TextBoxSetCaretMessage(int caretIndex, int selectionLength)
        {
            this.CaretIndex = caretIndex;
            SelectionLength = selectionLength;
        }

        public TextBoxSetCaretMessage(string messageKey, int caretIndex, int selectionLength)
            : base(messageKey)
        {
            this.CaretIndex = caretIndex;
            SelectionLength = selectionLength;
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new TextBoxSetCaretMessage(this.MessageKey, CaretIndex, SelectionLength);
        }
    }
}
