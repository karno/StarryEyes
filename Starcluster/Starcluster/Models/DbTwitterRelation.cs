using Starcluster.Infrastructures;

namespace Starcluster.Models
{
    [DbUniqueColumn(nameof(UserId), nameof(TargetId))]
    public class DbTwitterRelation
    {
        public DbTwitterRelation()
        {
        }

        public DbTwitterRelation(long uid, long tid, RelationState state, bool isRetweetSuppressed)
        {
            UserId = uid;
            TargetId = tid;
            RelationState = state;
            IsRetweetSuppressed = state == RelationState.Following && isRetweetSuppressed;
        }

        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long UserId { get; set; }

        public long TargetId { get; set; }

        public RelationState RelationState { get; set; }

        public bool IsRetweetSuppressed { get; set; }
    }

    public enum RelationState
    {
        None,
        Following,
        Blocking,
    }
}