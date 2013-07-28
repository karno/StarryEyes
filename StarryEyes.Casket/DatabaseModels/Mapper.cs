using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Casket.DatabaseModels
{
    public static class Mapper
    {
        public static TwitterUser Map(DatabaseUser user)
        {
            throw new NotImplementedException();
        }

        public static Tuple<DatabaseUser, IEnumerable<DatabaseEntity>> Map(TwitterUser user)
        {
            throw new NotImplementedException();
        }
    }
}
