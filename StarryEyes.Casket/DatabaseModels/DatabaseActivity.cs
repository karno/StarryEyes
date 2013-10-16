using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbName("Activity"), DbUniqueColumn("StatusId", "UserId")]
    public sealed class DatabaseActivity
    {
        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long StatusId { get; set; }

        public long UserId { get; set; }
    }
}
