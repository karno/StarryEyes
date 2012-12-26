using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                    lock (StatisticsWorkProcSync)
                    {
                        Monitor.Pulse(StatisticsWorkProcSync);
                    }
                });
            estimatedGrossTweetCount = StatusStore.Count;
            App.OnApplicationFinalize += StopThread;
            Task.Factory.StartNew(UpdateStatisticWorkProc, TaskCreationOptions.LongRunning);
        }

        private static DateTime _timestamp = DateTime.Now;

        private static readonly object StatisticsWorkProcSync = new object();
        private static volatile bool _isThreadAlive = true;
        private static void StopThread()
        {
            lock (StatisticsWorkProcSync)
            {
                _isThreadAlive = false;
                Monitor.Pulse(StatisticsWorkProcSync);
            }
        }

        /// <summary>
        /// Work procedure
        /// </summary>
        private static void UpdateStatisticWorkProc()
        {
            while (_isThreadAlive)
            {
                lock (StatisticsWorkProcSync)
                {
                    if (!_isThreadAlive) return;
                    Monitor.Wait(StatisticsWorkProcSync);
                }
                if (!_isThreadAlive) return;
                // update statistics params
                var previousGross = estimatedGrossTweetCount;
                estimatedGrossTweetCount = StatusStore.Count;
                var previousTimestamp = _timestamp;
                _timestamp = DateTime.Now;
                var duration = (_timestamp - previousTimestamp).TotalSeconds;
                if (duration > 0)
                {
                    // current period of tweets per seconds
                    var cptps = (estimatedGrossTweetCount - previousGross) / duration;
                    // smoothing: 119:1
                    // -> 
                    tweetsPerSeconds = (tweetsPerSeconds * 119 + cptps) / 120;
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
