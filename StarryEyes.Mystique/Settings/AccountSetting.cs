using System.Runtime.Serialization;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Settings
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
    }
}
