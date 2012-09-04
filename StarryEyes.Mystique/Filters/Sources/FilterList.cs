using System;
using StarryEyes.Mystique.Models.Connection.Polling;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Sources
{
    public class FilterList : FilterSourceBase
    {
        ListInfo _listInfo;
        public FilterList(string ownerAndslug)
        {
            var splited = ownerAndslug.Split('/');
            if (splited.Length != 2)
                throw new ArgumentException("owner and slug must be separated as slash, once.");
            _listInfo = new ListInfo() { OwnerScreenName = splited[0], Slug = splited[1] };
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            throw new NotImplementedException();
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? max_id)
        {
            // ListReceiver.DoReceive(
            return base.ReceiveSink(max_id);
        }

        public override string FilterKey
        {
            get { return "list"; }
        }

        public override string FilterValue
        {
            get { return _listInfo.OwnerScreenName + "/" + _listInfo.Slug; }
        }

        private bool isActivated = false;
        public override void Activate()
        {
            if (isActivated) return;
            isActivated = true;
            ListReceiver.StartReceive(_listInfo);
        }

        public override void Deactivate()
        {
            if (!isActivated) return;
            isActivated = false;
            ListReceiver.StopReceive(_listInfo);
        }
    }
}
