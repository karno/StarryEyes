using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Subsystems
{
    /// <summary>
    /// Post limit prediction subsystem
    /// </summary>
    public static class PostLimitPredictionService
    {
        private static readonly object _dictLock = new object();
        private static readonly SortedDictionary<long, LinkedList<TwitterStatus>> _dictionary =
            new SortedDictionary<long, LinkedList<TwitterStatus>>();

        public static void Initialize()
        {
            Setting.Accounts.Collection.ListenCollectionChanged().Subscribe(AccountsChanged);
            Setting.Accounts.Collection.ForEach(SetupAccount);
            StatusStore.StatusPublisher
                       .Where(d => d.IsAdded)
                       .Select(d => d.Status)
                       .Subscribe(PostDetected);
        }

        private static void AccountsChanged(NotifyCollectionChangedEventArgs e)
        {
            var added = e.NewItems != null ? e.NewItems[0] as TwitterAccount : null;
            var removed = e.OldItems != null ? e.OldItems[0] as TwitterAccount : null;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SetupAccount(added);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveAccount(removed);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveAccount(removed);
                    SetupAccount(added);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _dictionary.Clear();
                    Setting.Accounts.Collection.ForEach(SetupAccount);
                    break;
            }
        }

        private static void SetupAccount(TwitterAccount info)
        {
            lock (_dictLock)
            {
                if (_dictionary.ContainsKey(info.Id)) return;
                _dictionary.Add(info.Id, new LinkedList<TwitterStatus>());
                StatusStore.FindBatch(s => s.User.Id == info.Id, Setting.PostLimitPerWindow.Value)
                           .Subscribe(PostDetected);
            }
        }

        private static void RemoveAccount(TwitterAccount info)
        {
            lock (_dictLock)
            {
                _dictionary.Remove(info.Id);
            }
        }

        private static void PostDetected(TwitterStatus status)
        {
            // check timestamp
            if (status.CreatedAt < DateTime.Now - TimeSpan.FromSeconds(Setting.PostWindowTimeSec.Value))
            {
                return;
            }

            // lookup chunk
            LinkedList<TwitterStatus> statuses;
            lock (_dictLock)
            {
                if (!_dictionary.TryGetValue(status.User.Id, out statuses))
                {
                    return;
                }
            }

            lock (statuses)
            {
                // find insert position
                var afterThis =
                    statuses.First == null
                        ? null
                        : EnumerableEx.Generate(statuses.First, n => n.Next != null, n => n.Next, n => n)
                                      .TakeWhile(n => n.Value.Id >= status.Id)
                                      .LastOrDefault();
                if (afterThis == null)
                {
                    statuses.AddFirst(status);
                }
                else if (afterThis.Value.Id == status.Id)
                {
                    return;
                }
                else
                {
                    statuses.AddAfter(afterThis, status);
                }
                LinkedListNode<TwitterStatus> last;
                var stamp = DateTime.Now - TimeSpan.FromSeconds(Setting.PostWindowTimeSec.Value);
                while ((last = statuses.Last) != null)
                {
                    if (last.Value.CreatedAt >= stamp)
                    {
                        break;
                    }
                    // timeout
                    statuses.RemoveLast();
                }
            }
        }

        public static int GetCurrentWindowCount(long id)
        {
            lock (_dictLock)
            {
                LinkedList<TwitterStatus> statuses;
                if (!_dictionary.TryGetValue(id, out statuses))
                {
                    return -1;
                }
                return statuses.Count;
            }
        }
    }
}
