using System;
using System.Data.SQLite;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Casket;
using StarryEyes.Models.Databases;

namespace StarryEyes.Models.Subsystems
{
    /// <summary>
    ///     Provides statistics functionalities.
    /// </summary>
    public static class StatisticsService
    {
        private static readonly object _statisticsWorkProcSync = new object();
        private static volatile bool _isThreadAlive = true;

        private static int _estimatedGrossTweetCount;

        private static int _tweetsPerMinutes;

        private static int _queuedStatusesCount;

        private static readonly int[] _tweetsCountArray = { 0, 0, 0, 0, 0, 0 };

        private static int _currentChannel = -1;

        /// <summary>
        ///     Gross tweet count (ESTIMATED, not ACTUAL)
        /// </summary>
        public static int EstimatedGrossTweetCount => _estimatedGrossTweetCount;

        /// <summary>
        ///     Tweets per seconds, estimated.
        /// </summary>
        public static int TweetsPerMinutes => _tweetsPerMinutes;

        public static void Initialize()
        {
            Observable.Interval(TimeSpan.FromSeconds(10))
                      .Subscribe(_ =>
                      {
                          lock (_statisticsWorkProcSync)
                          {
                              Monitor.Pulse(_statisticsWorkProcSync);
                          }
                      });
            App.ApplicationFinalize += StopThread;
            Task.Factory.StartNew(UpdateStatisticWorkProc, TaskCreationOptions.LongRunning);
            Task.Run(UpdateTweetCount);
        }

        private static async Task UpdateTweetCount()
        {
            try
            {
                var dbcount = (int)(await StatusProxy.GetCountAsync().ConfigureAwait(false));
                _estimatedGrossTweetCount = dbcount + _queuedStatusesCount;
            }
            catch (SqliteCrudException)
            {
            }
            catch (SQLiteException)
            {
            }
        }

        private static void StopThread()
        {
            lock (_statisticsWorkProcSync)
            {
                _isThreadAlive = false;
                Monitor.Pulse(_statisticsWorkProcSync);
            }
        }

        internal static void SetQueuedStatusCount(int count)
        {
            _queuedStatusesCount = count;
        }

        /// <summary>
        ///     Work procedure
        /// </summary>
        private static async void UpdateStatisticWorkProc()
        {
            while (_isThreadAlive)
            {
                lock (_statisticsWorkProcSync)
                {
                    if (!_isThreadAlive) return;
                    Monitor.Wait(_statisticsWorkProcSync);
                }
                if (!_isThreadAlive) return;

                // update statistics params
                var previousGross = _estimatedGrossTweetCount;
                await UpdateTweetCount().ConfigureAwait(false);
                var delta = _estimatedGrossTweetCount - previousGross;
                System.Diagnostics.Debug.WriteLine("status count: " + _estimatedGrossTweetCount + ", delta: " + delta);

                // indicate next channel
                _currentChannel = (_currentChannel + 1) % 6;

                _tweetsCountArray[_currentChannel] = delta;

                Interlocked.Exchange(ref _tweetsPerMinutes, _tweetsCountArray.Sum());

                StatisticsParamsUpdated?.Invoke();
            }
        }

        /// <summary>
        ///     Events callbacked when statistics parameters are updated.
        /// </summary>
        public static event Action StatisticsParamsUpdated;
    }
}