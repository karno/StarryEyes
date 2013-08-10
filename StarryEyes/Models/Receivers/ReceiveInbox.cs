using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Albireo.Data;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Notifications;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers
{
    public static class ReceiveInbox
    {
        private static IDisposable _cache;
        private static volatile bool _predicateInvalid = true;

        private static AVLTree<long> _blockings = new AVLTree<long>();

        private static readonly ManualResetEvent _signal = new ManualResetEvent(false);

        private static readonly ConcurrentQueue<TwitterStatus> _queue = new ConcurrentQueue<TwitterStatus>();

        private static Thread _pumpThread;
        private static Func<TwitterStatus, bool> _muteEval;

        public static void Queue(TwitterStatus status)
        {
            _queue.Enqueue(status);
            _signal.Set();
        }

        internal static void Initialize()
        {
            App.ApplicationExit += () => _pumpThread.Abort();
            _pumpThread = new Thread(PumpQueuedStatuses);
            _pumpThread.Start();
            Setting.Accounts.Collection.ListenCollectionChanged().Subscribe(_ => InvalidateBlockInfo());
            Setting.Muteds.ValueChanged += _ => InvalidateMutePredicate();
        }

        // ReSharper disable FunctionNeverReturns
        // ReSharper disable TooWideLocalVariableScope
        private static async void PumpQueuedStatuses()
        {
            TwitterStatus status;
            while (true)
            {
                _signal.Reset();
                while (_queue.TryDequeue(out status))
                {
                    if ((await StatusStore.Get(status.Id).DefaultIfEmpty()) != null) continue;
                    if (!ValidateStatus(status))
                    {
                        System.Diagnostics.Debug.WriteLine("MUTE OR BLOCK CAPTURE: " + status);
                        continue; // muted or blocked
                    }
                    if (status.RetweetedOriginal != null)
                    {
                        Queue(status.RetweetedOriginal);
                    }
                    StatusStore.Store(status);
                    NotificationModel.NotifyNewArrival(status);
                }
                _signal.WaitOne();

            }
        }
        // ReSharper restore TooWideLocalVariableScope
        // ReSharper restore FunctionNeverReturns

        private static bool ValidateStatus(TwitterStatus status)
        {
            if (Interlocked.CompareExchange(ref _cache, null, null) == null)
            {
                UpdateCache();
            }
            if (_predicateInvalid)
            {
                _muteEval = Setting.Muteds.Evaluator;
                _predicateInvalid = false;
            }
            if (_blockings.Contains(status.User.Id)) return false;
            if (_muteEval(status)) return false;

            return status.RetweetedOriginal == null || ValidateStatus(status.RetweetedOriginal);
        }

        private static void UpdateCache()
        {
            var disposables = new CompositeDisposable();
            var nbs = new AVLTree<long>();
            Setting.Accounts
        .Collection
                   .Select(a => a.RelationData)
                   .Do(r => disposables.Add(
                       Observable.FromEvent<RelationDataChangedInfo>(h => r.AccountDataUpdated += h,
                                                                     h => r.AccountDataUpdated -= h)
                                 .Where(info => info.Change == RelationDataChange.Blocking)
                                 .Subscribe(_ => InvalidateBlockInfo())))
                   .SelectMany(r => r.Blockings)
                   .ForEach(nbs.Add);
            _blockings = nbs;

            var oc = Interlocked.Exchange(ref _cache, disposables);
            if (oc != null)
            {
                oc.Dispose();
            }
        }

        private static void InvalidateBlockInfo()
        {
            var oc = Interlocked.Exchange(ref _cache, null);
            if (oc != null)
            {
                oc.Dispose();
            }
        }

        private static void InvalidateMutePredicate()
        {
            _predicateInvalid = true;
        }

    }
}
