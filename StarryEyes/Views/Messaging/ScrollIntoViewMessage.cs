using Livet;
using Livet.Messaging;

namespace StarryEyes.Views.Messaging
{
    public class ScrollIntoViewMessage : InteractionMessage
    {
        public ScrollIntoViewMessage(string messageKey, int index)
            : base(messageKey)
        {
            DispatcherHelper.UIDispatcher.VerifyAccess();
            Index = index;
        }

        public ScrollIntoViewMessage(int index)
            : this(null, index)
        {
        }

        public int Index { get; }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new ScrollIntoViewMessage(MessageKey, Index);
        }
    }
}