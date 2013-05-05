using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Notifications;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers
{
    public static class ReceiveInbox
    {
        private static volatile bool _cacheInvalid = true;
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
            AccountRelationDataStore.AccountDataUpdated += _ => InvalidateRelationInfo();
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
            if (_cacheInvalid)
            {
                var nbs = new AVLTree<long>();
                AccountRelationDataStore.AccountRelations
                                        .SelectMany(a => a.Blockings)
                                        .ForEach(nbs.Add);
                _blockings = nbs;
                _cacheInvalid = false;
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

        private static void InvalidateRelationInfo()
        {
            _cacheInvalid = true;
        }

        private static void InvalidateMutePredicate()
        {
            _predicateInvalid = true;
        }

    }
}
