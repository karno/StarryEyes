namespace Sophia.Messaging.UI
{
    public class ScrollIntoViewMessage : MessageBase
    {
        public int Index { get; }

        public ScrollIntoViewMessage(string key, int index) : base(key)
        {
            Index = index;
        }

        public ScrollIntoViewMessage(int index) : this(null, index)
        {
        }
    }
}