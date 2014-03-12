using System;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo.Collections;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Sources
{
    public class FilterList : FilterSourceBase
    {
        private readonly ListInfo _listInfo;

        private readonly string _receiverScreenName;

        private long _listId;

        // activation control
        private bool _isActivated;

        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            if (!String.IsNullOrEmpty(this._receiverScreenName))
            {
                ReceiveManager.RegisterList(this._receiverScreenName, _listInfo);
            }
            else
            {
                ReceiveManager.RegisterList(_listInfo);
            }
            ReceiveManager.ListMemberChanged += OnListMemberChanged;
            RefreshMembers();
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            ReceiveManager.ListMemberChanged -= OnListMemberChanged;
            ReceiveManager.UnregisterList(_listInfo);
        }

        private void OnListMemberChanged(ListInfo obj)
        {
            if (obj.Equals(this._listInfo))
            {
                RefreshMembers();
            }
        }

        #region Manage members

        private readonly AVLTree<long> _ids = new AVLTree<long>();

        private void RefreshMembers()
        {
            Task.Run(async () =>
            {
                var listDesc = await ListProxy.GetListDescription(_listInfo);
                if (listDesc != null)
                {
                    _listId = listDesc.Id;
                }
                var userIds = await ListProxy.GetListMembers(_listInfo);
                if (userIds == null) return;
                lock (_ids)
                {
                    _ids.Clear();
                    userIds.ForEach(id => _ids.Add(id));
                }
                RaiseInvalidateRequired();
            });
        }

        #endregion

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
                this._receiverScreenName = splited[0];
            }
            else
            {
                _listInfo = new ListInfo { OwnerScreenName = splited[1], Slug = splited[2] };
                this._receiverScreenName = splited[0];
            }
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

        public override string GetSqlQuery()
        {
            return "exists (select * from ListUser where ListId = " + _listId + " and UserId = status.UserId limit 1)";
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return this.GetAccount()
                       .GetListTimelineAsync(this._listInfo.Slug, this._listInfo.OwnerScreenName, maxId: maxId)
                       .ToObservable();
        }

        private TwitterAccount GetAccount()
        {
            return Setting.Accounts.Collection
                          .FirstOrDefault(
                              a => this._receiverScreenName.Equals(a.UnreliableScreenName,
                                  StringComparison.CurrentCultureIgnoreCase)) ??
                   Setting.Accounts.GetRandomOne();
        }

        public override string FilterKey
        {
            get { return "list"; }
        }

        public override string FilterValue
        {
            get
            {
                return this._receiverScreenName == this._listInfo.OwnerScreenName
                    ? this._listInfo.OwnerScreenName + "/" + this._listInfo.Slug
                    : this._receiverScreenName + "/" + this._listInfo.OwnerScreenName + "/" + this._listInfo.Slug;
            }
        }
    }
}
