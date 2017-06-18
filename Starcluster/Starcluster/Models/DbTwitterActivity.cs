using Starcluster.Infrastructures;

namespace Starcluster.Models
{
    public class DbTwitterActivity
    {
        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long StatusId { get; set; }

        public long UserId { get; set; }
    }
}