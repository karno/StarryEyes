using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;

namespace StarryEyes.Settings
{
    public class AccountManager
    {
        private readonly Setting.SettingItem<List<TwitterAccount>> _settingItem;

        private readonly ObservableSynchronizedCollectionEx<TwitterAccount> _accountObservableCollection;

        private readonly ConcurrentDictionary<long, TwitterAccount> _accountCache;

        private readonly Random _random;

        internal AccountManager(Setting.SettingItem<List<TwitterAccount>> settingItem)
        {
            if (settingItem == null)
            {
                throw new ArgumentNullException("settingItem");
            }
            this._settingItem = settingItem;
            _accountObservableCollection = new ObservableSynchronizedCollectionEx<TwitterAccount>(_settingItem.Value);
            _accountCache = new ConcurrentDictionary<long, TwitterAccount>(
                _accountObservableCollection.Distinct(a => a.Id).ToDictionary(a => a.Id));
            _accountObservableCollection.CollectionChanged += CollectionChanged;
            _random = new Random(Environment.TickCount);
            // initialize DB after system is available.
            App.SystemReady += this.SynchronizeDb;
        }

        private async void SynchronizeDb()
        {
            var accounts = (await AccountProxy.GetAccountsAsync()).ToArray();
            var registered = _accountCache.Keys.ToArray();
            var news = registered.Except(accounts).ToArray();
            var olds = accounts.Except(registered).ToArray();
            await Task.Run(() => Task.WaitAll(
                olds.Select(AccountProxy.RemoveAccountAsync)
                    .Concat(news.Select(AccountProxy.AddAccountAsync))
                    .ToArray()));
        }

        public IEnumerable<long> Ids
        {
            get { return _accountCache.Keys; }
        }

        public ObservableSynchronizedCollectionEx<TwitterAccount> Collection
        {
            get { return _accountObservableCollection; }
        }

        public bool Contains(long id)
        {
            return _accountCache.ContainsKey(id);
        }

        public TwitterAccount Get(long id)
        {
            TwitterAccount account;
            return this._accountCache.TryGetValue(id, out account) ? account : null;
        }

        public TwitterAccount GetRandomOne()
        {
            var accounts = _accountObservableCollection.ToArray();
            return accounts.Length == 0
                       ? null
                       : accounts[this._random.Next(accounts.Length)];
        }

        public TwitterAccount GetRelatedOne(long id)
        {
            if (Setting.Accounts.Contains(id))
            {
                return this.Get(id);
            }
            var followings = Setting.Accounts.Collection
                                    .Where(a => a.RelationData.IsFollowing(id))
                                    .ToArray();
            return followings.Length == 0
                       ? this.GetRandomOne()
                       : followings[this._random.Next(followings.Length)];
        }

        public void RemoveAccountFromId(long id)
        {
            var acc = this.Get(id);
            if (acc != null) _accountObservableCollection.Remove(acc);
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var added = e.NewItems != null ? e.NewItems[0] as TwitterAccount : null;
            var removed = e.OldItems != null ? e.OldItems[0] as TwitterAccount : null;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (added == null) throw new ArgumentException("added item is null.");
                    _accountCache[added.Id] = added;
                    Task.Run(() => AccountProxy.AddAccountAsync(added.Id));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (removed == null) throw new ArgumentException("removed item is null.");
                    TwitterAccount removal;
                    _accountCache.TryRemove(removed.Id, out removal);
                    Task.Run(() => AccountProxy.RemoveAccountAsync(removed.Id));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (added == null) throw new ArgumentException("added item is null.");
                    if (removed == null) throw new ArgumentException("removed item is null.");
                    _accountCache[added.Id] = added;
                    TwitterAccount replacee;
                    _accountCache.TryRemove(removed.Id, out replacee);
                    Task.Run(() => AccountProxy.AddAccountAsync(added.Id));
                    Task.Run(() => AccountProxy.RemoveAccountAsync(removed.Id));
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _accountCache.Clear();
                    _accountObservableCollection.ForEach(a => _accountCache[a.Id] = a);
                    Task.Run(() => AccountProxy.RemoveAllAccountsAsync());
                    break;
            }
            _settingItem.Value = _accountObservableCollection.ToList();
        }
    }
}
