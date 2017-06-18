using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Receivers
{
    public class SearchReceiver : CyclicReceiverBase
    {
        private readonly string _query;
        private readonly IEnumerable<ICollection<long>> _receiveCaches;

        public SearchReceiver(string query, IEnumerable<ICollection<long>> receiveCaches)
        {
            this._query = query;
            this._receiveCaches = receiveCaches;
        }

        protected override string ReceiverName
        {
            get { return ReceivingResources.ReceiverSearchFormat.SafeFormat(_query); }
        }

        protected override int IntervalSec
        {
            get { return Setting.RESTSearchReceivePeriod.Value; }
        }

        protected override async Task DoReceive()
        {
            var account = Setting.Accounts.GetRandomOne();
            if (account == null)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent(
                    ReceivingResources.AccountIsNotRegisteredForSearch, null));
                return;
            }
            System.Diagnostics.Debug.WriteLine("SEARCHRECEIVER SEARCH QUERY: " + this._query);
            (await account.SearchAsync(this._query,
                lang: String.IsNullOrWhiteSpace(Setting.SearchLanguage.Value) ? null : Setting.SearchLanguage.Value,
                locale: String.IsNullOrWhiteSpace(Setting.SearchLocale.Value) ? null : Setting.SearchLocale.Value))
                .Do(s =>
                {
                    lock (_receiveCaches)
                    {
                        _receiveCaches.ForEach(c => c.Add(s.Id));
                    }
                })
                .ForEach(StatusInbox.Enqueue);
        }
    }
}
