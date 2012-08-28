using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.Mystique.Models.Connection.Polling;
using StarryEyes.SweetLady.DataModel;
using System.Reactive.Linq;

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
    }
}
