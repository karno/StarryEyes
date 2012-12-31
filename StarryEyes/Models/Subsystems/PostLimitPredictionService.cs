using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }

        private static void AccountsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var aset = (AccountSetting)e.NewItems[0];
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
                return;

            // lookup chunk
            LinkedList<TwitterStatus> statuses;
            lock (_dictLock)
            {
                if (!_dictionary.TryGetValue(status.User.Id, out statuses)) return;
            }

            lock (statuses)
            {
                // find insert position
                var afterThis = statuses.FirstOrDefault(t => t.Id <= status.Id);
                // check duplication
                if (afterThis.Id == status.Id) return;
            }
        }
    }
}
