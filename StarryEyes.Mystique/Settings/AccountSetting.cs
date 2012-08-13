using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.SweetLady.Authorize;
using System.Runtime.Serialization;

namespace StarryEyes.Mystique.Settings
{
    [DataContract]
    public class AccountSetting
    {
        [DataMember]
        public AuthenticateInfo AuthenticateInfo { get; set; }
    }
}
