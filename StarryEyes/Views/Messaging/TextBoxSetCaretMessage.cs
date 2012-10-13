using Livet.Messaging;

namespace StarryEyes.Views.Messaging
{
    public class TextBoxSetCaretMessage : InteractionMessage
    {
        public int CaretIndex { get; set; }
        public TextBoxSetCaretMessage() :base() { }
        public TextBoxSetCaretMessage(string messageKey) : base(messageKey) { }
        public TextBoxSetCaretMessage(int caretIndex)
            : base()
        {
            this.CaretIndex = caretIndex;
        }
        public TextBoxSetCaretMessage(string messageKey, int caretIndex)
            : base(messageKey)
        {
            this.CaretIndex = caretIndex;
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new TextBoxSetCaretMessage(this.MessageKey, CaretIndex);
        }
    }
}
