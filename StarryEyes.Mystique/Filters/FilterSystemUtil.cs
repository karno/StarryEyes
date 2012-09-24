using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters
{
    public static class FilterSystemUtil
    {
        public static IEnumerable<long> InReplyToUsers(TwitterStatus status)
        {
            if (status.Entities == null)
                return Enumerable.Empty<long>();
            else
                return status.Entities
                    .Where(e => e.EntityType == EntityType.UserMentions)
                    .Select(e =>
                    {
                        try
                        {
                            return long.Parse(e.OriginalText);
                        }
                        catch
                        {
                            return 0;
                        }
                    }).Where(_ => _ != 0);
        }

        public static TwitterStatus GetOriginal(this TwitterStatus status)
        {
            return status.RetweetedOriginal ?? status;
        }
    }
}
