using System;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Models.Store;

namespace StarryEyes.Models.Hub
{
    /// <summary>
    /// Provides statistics functionalities.
    /// </summary>
    public static class StatisticsHub
    {
        public static void Initialize()
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
            workThread.Start();
        }

        private static DateTime timestamp = DateTime.Now;

        private static Thread workThread;
        private static object statisticsWorkProcSync = new object();
        private static volatile bool isThreadAlive = true;
        private static void StopThread()
        {
            lock (statisticsWorkProcSync)
            {
                isThreadAlive = false;
                Monitor.Pulse(statisticsWorkProcSync);
            }
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
                    if (!isThreadAlive) return;
                    Monitor.Wait(statisticsWorkProcSync);
                }
                if (!isThreadAlive) return;
                // update statistics params
                var previousGross = estimatedGrossTweetCount;
                estimatedGrossTweetCount = StatusStore.Count;
                var previousTimestamp = timestamp;
                timestamp = DateTime.Now;
                var duration = (timestamp - previousTimestamp).TotalSeconds;
                if (duration > 0)
                {
                    // current period of tweets per seconds
                    var cptps = (estimatedGrossTweetCount - previousGross) / duration;
                    // smoothing: 99.2:0.8
                    tweetsPerSeconds = (tweetsPerSeconds * 0.992) + (cptps * 0.008);
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
            get { return StatisticsHub.estimatedGrossTweetCount; }
        }

        private static double tweetsPerSeconds = 0;
        /// <summary>
        /// Tweets per seconds, estimated.
        /// </summary>
        public static double TweetsPerSeconds
        {
            get { return StatisticsHub.tweetsPerSeconds; }
        }
    }
}
