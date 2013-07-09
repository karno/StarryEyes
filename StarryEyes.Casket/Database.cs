
using StarryEyes.Casket.Rdb;

namespace StarryEyes.Casket
{
    public static class Database
    {
        public static void Initialize(string dbfilepath, string kvsfilepath)
        {
            Sqlite.Initialize(dbfilepath);
        }
    }
}
