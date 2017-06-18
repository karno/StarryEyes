using JetBrains.Annotations;

namespace Sophia.Messaging
{
    /// <summary>
    /// Base message of messaging system.
    /// </summary>
    public class MessageBase
    {
        /// <summary>
        /// Message key (if not specified, set as String.Empty or null.)
        /// </summary>
        [CanBeNull]
        public string Key { get; set; }

        public MessageBase([CanBeNull] string key = null)
        {
            Key = key;
        }
    }
}