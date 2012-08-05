using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarryEyes.Mystique.Filters.Core
{
    /// <summary>
    /// Tweets source of status
    /// </summary>
    public abstract class SourceBase
    {
        public abstract string ToQuery();
    }
}
