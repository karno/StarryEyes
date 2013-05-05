using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Notifications;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Receivers
{
    public static class ReceiveInbox
    {
        private static readonly ManualResetEvent _signal = new ManualResetEvent(false);

        private static readonly ConcurrentQueue<TwitterStatus> _queue = new ConcurrentQueue<TwitterStatus>();

        private static Thread _pumpThread;

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
    }
}
