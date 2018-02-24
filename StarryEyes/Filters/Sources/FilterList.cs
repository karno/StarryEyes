using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Data;
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
                _receiverScreenName = splited[0];
            }
            else
            {
                _listInfo = new ListInfo(splited[1], splited[2]);
                _receiverScreenName = splited[0];
            }
            _watcher = new ListWatcher(_listInfo);
            System.Diagnostics.Debug.WriteLine("#INVALIDATION: List Information Loaded");
            _watcher.OnListMemberUpdated += RaiseInvalidateRequired;
        }

        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            _watcher.Activate();
            if (!String.IsNullOrEmpty(_receiverScreenName))
            {
                ReceiveManager.RegisterList(_receiverScreenName, _listInfo);
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
                if (_watcher?.Ids == null) return false;
                lock (_watcher.Ids)
                {
                    return _watcher.Ids.Contains(s.User.Id);
                }
            };
        }

        public override string GetSqlQuery()
        {
            return "exists (select * from ListUser where ListId = " + _watcher.ListId +
                   " and UserId = status.UserId limit 1)";
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return GetAccount().CreateAccessor()
                               .GetListTimelineAsync(new ListParameter(_listInfo.OwnerScreenName, _listInfo.Slug),
                                   null, maxId, 100, true, CancellationToken.None)
                               .ToObservable().SelectMany(o => o.Result);
        }

        private TwitterAccount GetAccount()
        {
            return Setting.Accounts.Collection
                          .FirstOrDefault(
                              a => _receiverScreenName.Equals(a.UnreliableScreenName,
                                  StringComparison.CurrentCultureIgnoreCase)) ??
                   Setting.Accounts.GetRandomOne();
        }

        public override string FilterKey => "list";

        public override string FilterValue
        {
            get
            {
                return _receiverScreenName == _listInfo.OwnerScreenName
                    ? _listInfo.OwnerScreenName + "/" + _listInfo.Slug
                    : _receiverScreenName + "/" + _listInfo.OwnerScreenName + "/" + _listInfo.Slug;
            }
        }
    }
}