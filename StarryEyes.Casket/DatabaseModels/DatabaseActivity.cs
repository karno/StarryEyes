using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbUniqueColumn("StatusId", "UserId")]
    public sealed class DatabaseActivity : DbModelBase
    {
        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long StatusId { get; set; }

        public long UserId { get; set; }
    }
}
