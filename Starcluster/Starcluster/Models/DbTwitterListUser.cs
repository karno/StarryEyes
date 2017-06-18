using Starcluster.Infrastructures;

namespace Starcluster.Models
{
    public class DbTwitterListUser
    {
        public DbTwitterListUser()
        {
        }

        public DbTwitterListUser(long listId, long userId)
        {
            ListId = listId;
            UserId = userId;
        }

        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long ListId { get; set; }

        public long UserId { get; set; }
    }
}