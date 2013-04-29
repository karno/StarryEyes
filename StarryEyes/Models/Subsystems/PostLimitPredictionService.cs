using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
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
            AccountsStore.Accounts.CollectionChanged += AccountsOnCollectionChanged;
            AccountsStore.Accounts.ForEach(a => SetupAccount(a.AuthenticateInfo));
            StatusStore.StatusPublisher
                       .Where(d => d.IsAdded)
                       .Select(d => d.Status)
                       .Subscribe(PostDetected);
        }

        private static void AccountsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var target = ((AccountSetting)e.NewItems[0]).AuthenticateInfo;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SetupAccount(target);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveAccount(target);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void SetupAccount(AuthenticateInfo info)
        {
            lock (_dictLock)
            {
                if (_dictionary.ContainsKey(info.Id)) return;
                _dictionary.Add(info.Id, new LinkedList<TwitterStatus>());
                StatusStore.FindBatch(s => s.User.Id == info.Id, Setting.PostLimitPerWindow.Value)
                           .Subscribe(PostDetected);
            }
        }

        private static void RemoveAccount(AuthenticateInfo info)
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
                                      .FirstOrDefault(n => n.Value.Id <= status.Id);
                // check duplication
                if (afterThis == null)
                {
                    if (statuses.First == null || statuses.First.Value.Id < status.Id)
                    {
                        statuses.AddFirst(status);
                    }
                    else
                    {
                        statuses.AddLast(status);
                    }
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
