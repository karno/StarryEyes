using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Receivers;
using StarryEyes.Models.Receivers.ReceiveElements;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Sources
{
    public class FilterList : FilterSourceBase
    {
        private readonly string _receiver;
        private readonly ListInfo _listInfo;
        private readonly AVLTree<long> _ids = new AVLTree<long>();

        public FilterList(string ownerAndslug)
        {
            var splited = ownerAndslug.Split('/');
            if (splited.Length < 2 || splited.Length > 3)
            {
                throw new ArgumentException("owner and slug must be separated as slash, once.");
            }
            if (splited.Length == 2)
            {
                _listInfo = new ListInfo { OwnerScreenName = splited[0], Slug = splited[1] };
                _receiver = splited[0];
            }
            else
            {
                _listInfo = new ListInfo { OwnerScreenName = splited[1], Slug = splited[2] };
                _receiver = splited[0];
            }
            this.GetListUsersInfo();
        }

        private void GetListUsersInfo(bool enforceReceive = false)
        {
            IEnumerable<long> users;
            if (!enforceReceive && (users = CacheStore.GetListUsers(_listInfo)).Any())
            {
                lock (_ids)
                {
                    users.ForEach(_ids.Add);
                }
                return;
            }
            var info = this.GetAccount();
            if (info == null) return;
            var newList = new List<long>();
            info.AuthenticateInfo.GetListMembersAll(slug: _listInfo.Slug, owner_screen_name: _listInfo.OwnerScreenName)
                .Subscribe(
                    l => newList.Add(l.Id),
                    ex => { },
                    () =>
                    {
                        if (newList.Count <= 0) return;
                        CacheStore.SetListUsers(this._listInfo, newList);
                        lock (this._ids)
                        {
                            this._ids.Clear();
                            newList.ForEach(this._ids.Add);
                        }
                    });
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return s =>
            {
                lock (_ids)
                {
                    return _ids.Contains(s.User.Id);
                }
            };
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            var info = this.GetAccount();
            return info == null
                       ? base.ReceiveSink(maxId)
                       : ListReceiver.DoReceive(info.AuthenticateInfo, this._listInfo, maxId);
        }

        private AccountSetting GetAccount()
        {
            return AccountsStore
                .Accounts
                .Where(a => a.AuthenticateInfo.UnreliableScreenName == _receiver)
                .Concat(AccountsStore.Accounts)
                .FirstOrDefault();
        }

        public override string FilterKey
        {
            get { return "list"; }
        }

        public override string FilterValue
        {
            get
            {
                return this._receiver == this._listInfo.OwnerScreenName
                           ? this._listInfo.OwnerScreenName + "/" + this._listInfo.Slug
                           : this._receiver + "/" + this._listInfo.OwnerScreenName + "/" + this._listInfo.Slug;
            }
        }

        private Timer _timer;
        private bool _isActivated;

        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            if (!String.IsNullOrEmpty(_receiver))
            {
                ReceiversManager.RegisterList(_receiver, _listInfo);
            }
            else
            {
                ReceiversManager.RegisterList(_listInfo);
            }
            _timer = new Timer(_ => this.TimerCallback(), null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(30));
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            ReceiversManager.UnregisterList(_listInfo);
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void TimerCallback()
        {
            if (!_isActivated)
            {
                _timer.Dispose();
                _timer = null;
                return;
            }
            this.GetListUsersInfo(true);
        }
    }
}
