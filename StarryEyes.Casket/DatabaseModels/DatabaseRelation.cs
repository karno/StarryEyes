using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbUniqueColumn("UserId", "TargetId")]
    public sealed class DatabaseRelation
    {
        public DatabaseRelation() { }

        public DatabaseRelation(long uid, long tid)
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
