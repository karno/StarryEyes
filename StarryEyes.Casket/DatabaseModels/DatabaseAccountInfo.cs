using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbName("Accounts")]
    public class DatabaseAccountInfo
    {
        public DatabaseAccountInfo(long id)
        {
            this.Id = id;
        }

        [DbPrimaryKey(false)]
        public long Id { get; set; }
    }
}
