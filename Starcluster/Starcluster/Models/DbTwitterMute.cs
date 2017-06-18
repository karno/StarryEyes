using Starcluster.Infrastructures;

namespace Starcluster.Models
{
    [DbUniqueColumn(nameof(UserId), nameof(TargetId))]
    public class DbTwitterMute
    {
        public DbTwitterMute()
        {
        }

        public DbTwitterMute(long uid, long tid)
        {
            UserId = uid;
            TargetId = tid;
        }

        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long UserId { get; set; }

        public long TargetId { get; set; }
    }
}