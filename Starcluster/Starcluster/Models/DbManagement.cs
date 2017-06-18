using Starcluster.Infrastructures;

namespace Starcluster.Models
{
    public sealed class DbManagement
    {
        public DbManagement()
        {
        }

        public DbManagement(long id, string value)
        {
            Id = id;
            Value = value;
        }

        [DbPrimaryKey]
        public long Id { get; set; }

        public string Value { get; set; }
    }
}