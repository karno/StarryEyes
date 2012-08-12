using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Connection.Polling
{
    /// <summary>
    /// Periodically receiving task
    /// </summary>
    public abstract class PollingConnectionBase : ConnectionBase
    {
        public PollingConnectionBase(AuthenticateInfo ai) : base(ai) { }
    }
}
