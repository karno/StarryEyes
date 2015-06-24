using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receiving;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Sources
{
    public class FilterList : FilterSourceBase
    {
        private readonly ListInfo _listInfo;

        private readonly string _receiverScreenName;

        private readonly ListWatcher _watcher;

        // activation control
        private bool _isActivated;

        public FilterList(string ownerAndslug)
        {
            var splited = ownerAndslug.Split('/');
            if (splited.Length < 2 || splited.Length > 3)
            {
                throw new ArgumentException("owner and slug must be separated as slash, once.");
            }
            if (splited.Length == 2)
            {
                _listInfo = new ListInfo(splited[0], splited[1]);
                this._receiverScreenName = splited[0];
            }
            else
            {
                _listInfo = new ListInfo(splited[1], splited[2]);
                this._receiverScreenName = splited[0];
            }
            _watcher = new ListWatcher(_listInfo);
            System.Diagnostics.Debug.WriteLine("#INVALIDATION: List Information Loaded");
            _watcher.OnListMemberUpdated += this.RaiseInvalidateRequired;
        }

        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            _watcher.Activate();
            if (!String.IsNullOrEmpty(this._receiverScreenName))
            {
                ReceiveManager.RegisterList(this._receiverScreenName, _listInfo);
            }
            else
            {
                ReceiveManager.RegisterList(_listInfo);
            }
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            _watcher.Deactivate();
            ReceiveManager.UnregisterList(_listInfo);
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return s =>
            {
                lock (_watcher.Ids)
                {
                    return this._watcher.Ids.Contains(s.User.Id);
                }
            };
        }

        public override string GetSqlQuery()
        {
            return "exists (select * from ListUser where ListId = " + _watcher.ListId + " and UserId = status.UserId limit 1)";
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return this.GetAccount()
                       .GetListTimelineAsync(ApiAccessProperties.Default, _listInfo.ToListParameter(), maxId: maxId)
                       .ToObservable().SelectMany(s => s.Result);
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
