using System;
using StarryEyes.Casket.DatabaseModels.Generators;

namespace StarryEyes.Casket.DatabaseModels
{
    public class DatabaseActivity : DbModelBase
    {
        [DbPrimaryKey]
        public long StatusId { get; set; }

        public long UserId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
