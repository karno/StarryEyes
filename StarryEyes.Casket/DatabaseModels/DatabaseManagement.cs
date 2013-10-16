using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbName("Management")]
    public class DatabaseManagement 
    {
        public DatabaseManagement() { }

        public DatabaseManagement(long id, string value)
        {
            Id = id;
            Value = value;
        }

        [DbPrimaryKey(false)]
        public long Id { get; set; }

        public string Value { get; set; }
    }
}
