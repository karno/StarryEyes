
using StarryEyes.Casket.SQLiteInternal;

namespace StarryEyes.Casket
{
    public static class Core
    {
        private static SQLiteInitializer _initializer;

        public static void Initialize(string basePath)
        {
            _initializer = new SQLiteInitializer(basePath);
        }
    }
}
