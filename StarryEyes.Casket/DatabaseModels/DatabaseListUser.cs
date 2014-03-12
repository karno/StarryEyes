using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbName("ListUser"), DbUniqueColumn("ListId", "UserId")]
    public class DatabaseListUser
    {
        public DatabaseListUser() { }

        public DatabaseListUser(long listId, long uid)
        {
            ListId = listId;
            UserId = uid;
        }

        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long ListId { get; set; }

        public long UserId { get; set; }
    }
}
