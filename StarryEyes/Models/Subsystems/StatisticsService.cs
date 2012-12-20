using System;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Subsystems
{
    /// <summary>
    /// Provides statistics functionalities.
    /// </summary>
    public static class StatisticsService
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
            estimatedGrossTweetCount = StatusStore.Count;
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
                    // smoothing: 59:1
                    // -> 
                    tweetsPerSeconds = (tweetsPerSeconds * 59 + cptps) / 60;
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
            get { return StatisticsService.estimatedGrossTweetCount; }
        }

        private static double tweetsPerSeconds = 0;
        /// <summary>
        /// Tweets per seconds, estimated.
        /// </summary>
        public static double TweetsPerSeconds
        {
            get { return StatisticsService.tweetsPerSeconds; }
        }
    }
}
