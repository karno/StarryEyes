using Livet;
using Livet.Messaging;

namespace StarryEyes.Views.Messaging
{
    public class ScrollIntoViewMessage : InteractionMessage
    {
        private readonly int _index;

        public ScrollIntoViewMessage(string messageKey, int index)
            : base(messageKey)
        {
            DispatcherHelper.UIDispatcher.VerifyAccess();
            _index = index;
        }

        public ScrollIntoViewMessage(int index)
            : this(null, index)
        {
        }

        public int Index
        {
            get { return _index; }
        }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new ScrollIntoViewMessage(this.MessageKey, this.Index);
        }
    }
}
