using System.Runtime.Serialization;
using StarryEyes.Moon.Authorize;

namespace StarryEyes.Settings
{
    /// <summary>
    /// Represents settings for the accounts.
    /// </summary>
    [DataContract]
    public class AccountSetting
    {
        /// <summary>
        /// User ID
        /// </summary>
        [DataMember]
        public long UserId
        {
            get { return AuthenticateInfo.Id; }
        }

        /// <summary>
        /// Authentication information
        /// </summary>
        [DataMember]
        public AuthenticateInfo AuthenticateInfo { get; set; }

        /// <summary>
        /// Flag of streaming connection is enabled
        /// </summary>
        [DataMember]
        public bool IsUserStreamsEnabled { get; set; }

        /// <summary>
        /// If set 0, fallback is disabled.
        /// </summary>
        [DataMember]
        public long FallbackNext { get; set; }
    }
}
