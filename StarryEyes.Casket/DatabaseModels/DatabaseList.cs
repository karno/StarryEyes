using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbName("List")]
    public sealed class DatabaseList
    {
        public DatabaseList() { }

        [DbPrimaryKey(false)]
        public long Id { get; set; }

        public long UserId { get; set; }

        public string Name { get; set; }

        public string FullName { get; set; }

        public string Slug { get; set; }
    }
}
