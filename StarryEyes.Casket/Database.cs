using System;
using System.Threading.Tasks;
using StarryEyes.Albireo.Threading;

namespace StarryEyes.Casket
{
    public static class Database
    {
        static readonly LimitedTaskScheduler _scheduler = new LimitedTaskScheduler(16);
        static readonly TaskFactory _factory = new TaskFactory(_scheduler);

        private static string _basePath;
        private static bool _isInitialized;

        public static string BasePath { get { return _basePath; } }

        public static void Initialize(string basePath)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("Database core is already initialized.");
            }
            _isInitialized = true;
            _basePath = basePath;
        }
    }
}
