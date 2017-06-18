using JetBrains.Annotations;

namespace Sophia.Messaging.UI
{
    public class BringIntoViewMessage : MessageBase
    {
        public BringIntoViewMessage([CanBeNull] string key = null) : base(key)
        {
        }
    }
}