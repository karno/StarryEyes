using System;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Mystique.Models.Store;

namespace StarryEyes.Mystique.Models.Hub
{
    /// <summary>
    /// Provides statistics functionalities.
    /// </summary>
    public static class StatisticHub
    {
        static StatisticHub()
        {
            Observable.Interval(TimeSpan.FromSeconds(0.5))
                .Subscribe(_ =>
                {
                    lock (statisticsWorkProcSync)
                    {
                        Monitor.Pulse(statisticsWorkProcSync);
                    }
                });
            App.OnApplicationFinalize += StopThread;
            workThread = new Thread(UpdateStatisticWorkProc);
        }

        private static DateTime timestamp = DateTime.Now;

        private static Thread workThread;
        private static object statisticsWorkProcSync = new object();
        private static bool isThreadAlive = true;
        private static void StopThread()
        {
            isThreadAlive = false;
        }

        private static void UpdateStatistics()
        {
            estimatedGrossTweetCount = StatusStore.Count;
        }

        /// <summary>
        /// Work procedure
        /// </summary>
        private static void UpdateStatisticWorkProc()
        {
            while (isThreadAlive)
            {
                lock (statisticsWorkProcSync)
                {
                    Monitor.Wait(statisticsWorkProcSync);
                }
                if (!isThreadAlive) return;
                // update statistics params
                var previousGross = estimatedGrossTweetCount;
                var previousTimestamp = DateTime.Now;
                estimatedGrossTweetCount = StatusStore.Count;
                timestamp = DateTime.Now;
                var duration = (timestamp - previousTimestamp).TotalSeconds;
                if (duration > 0)
                {
                    tweetsPerSeconds = (estimatedGrossTweetCount - previousGross) / duration;
                }
                var handler = OnStatisticsParamsUpdated;
                if (handler != null)
                    handler();
            }
        }

        /// <summary>
        /// Events callbacked when statistics parameters are updated.
        /// </summary>
        public static event Action OnStatisticsParamsUpdated;

        private static int estimatedGrossTweetCount = 0;
        /// <summary>
        /// Gross tweet count (ESTIMATED, not ACTUAL)
        /// </summary>
        public static int EstimatedGrossTweetCount
        {
            get { return StatisticHub.estimatedGrossTweetCount; }
        }

        private static double tweetsPerSeconds = 0;
        /// <summary>
        /// Tweets per seconds, estimated.
        /// </summary>
        public static double TweetsPerSeconds
        {
            get { return StatisticHub.tweetsPerSeconds; }
        }
    }
}
