using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Models.Databases;

namespace StarryEyes.Models.Subsystems
{
    /// <summary>
    ///     Provides statistics functionalities.
    /// </summary>
    public static class StatisticsService
    {
        private static readonly object StatisticsWorkProcSync = new object();
        private static volatile bool _isThreadAlive = true;

        private static int _estimatedGrossTweetCount;

        private static int _tweetsPerMinutes = 0;

        private static readonly int[] _tweetsCountArray = new[] { 0, 0, 0, 0, 0, 0 };

        private static int _currentChannel = -1;

        /// <summary>
        ///     Gross tweet count (ESTIMATED, not ACTUAL)
        /// </summary>
        public static int EstimatedGrossTweetCount
        {
            get { return _estimatedGrossTweetCount; }
        }

        /// <summary>
        ///     Tweets per seconds, estimated.
        /// </summary>
        public static int TweetsPerMinutes
        {
            get
            {
                return _tweetsPerMinutes;
            }
        }

        public static void Initialize()
        {
            Observable.Interval(TimeSpan.FromSeconds(10))
                      .Subscribe(_ =>
                      {
                          lock (StatisticsWorkProcSync)
                          {
                              Monitor.Pulse(StatisticsWorkProcSync);
                          }
                      });
            App.ApplicationFinalize += StopThread;
            Task.Factory.StartNew(UpdateStatisticWorkProc, TaskCreationOptions.LongRunning);
            UpdateTweetCount();
        }

        private static async Task UpdateTweetCount()
        {
            _estimatedGrossTweetCount = (int)(await StatusProxy.GetCountAsync());
        }

        private static void StopThread()
        {
            lock (StatisticsWorkProcSync)
            {
                _isThreadAlive = false;
                Monitor.Pulse(StatisticsWorkProcSync);
            }
        }

        /// <summary>
        ///     Work procedure
        /// </summary>
        private static async void UpdateStatisticWorkProc()
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
                var previousGross = _estimatedGrossTweetCount;
                await UpdateTweetCount();
                var delta = _estimatedGrossTweetCount - previousGross;

                // indicate next channel
                _currentChannel = (_currentChannel + 1) % 6;

                _tweetsCountArray[_currentChannel] = delta;

                Interlocked.Exchange(ref _tweetsPerMinutes, _tweetsCountArray.Sum());

                var handler = StatisticsParamsUpdated;
                if (handler != null)
                    handler();
            }
        }

        /// <summary>
        ///     Events callbacked when statistics parameters are updated.
        /// </summary>
        public static event Action StatisticsParamsUpdated;
    }
}