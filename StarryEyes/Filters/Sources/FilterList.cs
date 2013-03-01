using System;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Connections.Extends;
using StarryEyes.Models.Stores;

namespace StarryEyes.Filters.Sources
{
    public class FilterList : FilterSourceBase
    {
        private readonly ListInfo _listInfo;
        public FilterList(string ownerAndslug)
        {
            var splited = ownerAndslug.Split('/');
            if (splited.Length != 2)
                throw new ArgumentException("owner and slug must be separated as slash, once.");
            _listInfo = new ListInfo { OwnerScreenName = splited[0], Slug = splited[1] };
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            throw new NotImplementedException();
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            var info = AccountsStore.Accounts
                                    .Where(
                                        a =>
                                        a.AuthenticateInfo.UnreliableScreenName ==
                                        _listInfo.OwnerScreenName).Concat(AccountsStore.Accounts)
                                 .FirstOrDefault();
            if (info == null) return base.ReceiveSink(maxId);
            return base.ReceiveSink(maxId)
                       .Merge(ListReceiver.DoReceive(info.AuthenticateInfo, _listInfo, maxId));
        }

        public override string FilterKey
        {
            get { return "list"; }
        }

        public override string FilterValue
        {
            get { return _listInfo.OwnerScreenName + "/" + _listInfo.Slug; }
        }

        private bool _isActivated;
        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            ListReceiver.StartReceive(_listInfo);
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            ListReceiver.StopReceive(_listInfo);
        }
    }
}
