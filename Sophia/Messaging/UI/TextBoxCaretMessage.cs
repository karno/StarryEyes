
namespace Sophia.Messaging.UI
{
    public class TextBoxCaretMessage : MessageBase
    {
        public int? SelectionStart { get; }

        public int? SelectionLength { get; }

        public string SelectionReplacingText { get; }

        public TextBoxCaretMessage()
        {
        }

        public TextBoxCaretMessage(int start)
        {
            SelectionStart = start;
        }

        public TextBoxCaretMessage(int start, int length)
        {
            SelectionStart = start;
            SelectionLength = length;
        }

        public TextBoxCaretMessage(string replacing)
        {
            SelectionReplacingText = replacing;
        }

        public TextBoxCaretMessage(string key, int start)
            : base(key)
        {
            SelectionStart = start;
        }

        public TextBoxCaretMessage(string key, int start, int length)
            : base(key)
        {
            SelectionStart = start;
            SelectionLength = length;
        }

        public TextBoxCaretMessage(string key, string replacing)
            : base(key)
        {
            SelectionReplacingText = replacing;
        }
    }
}