using System.Collections.Generic;
using System.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Models.Backpanels.NotificationEvents;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Connections.Extends
{
    public sealed class SearchReceiver : PollingConnectionBase
    {
        private static readonly object SearchLocker = new object();
        private static readonly SortedDictionary<string, SearchReceiver> SearchReceiverResolver
            = new SortedDictionary<string, SearchReceiver>();
        private static readonly SortedDictionary<string, int> SearchReferenceCount
            = new SortedDictionary<string, int>();

        public static void RegisterSearchQuery(string query)
        {
            lock (SearchLocker)
            {
                if (SearchReferenceCount.ContainsKey(query))
                {
                    SearchReferenceCount[query]++;
                }
                else
                {
                    SearchReferenceCount.Add(query, 1);
                    var receiver = new SearchReceiver(query) { IsActivated = true };
                    SearchReceiverResolver.Add(query, receiver);
                }
            }
        }

        public static void RemoveSearchQuery(string query)
        {
            lock (SearchLocker)
            {
                if (!SearchReferenceCount.ContainsKey(query))
                    return;
                if (--SearchReferenceCount[query] == 0)
                {
                    SearchReferenceCount.Remove(query);
                    var receiver = SearchReceiverResolver[query];
                    SearchReceiverResolver.Remove(query);
                    receiver.Dispose();
                }
            }
        }

        private readonly string _query;
        private SearchReceiver(string query)
            : base(null)
        {
            this._query = query;
        }

        protected override int IntervalSec
        {
            get { return 60; }
        }

        protected override void DoReceive()
        {
            DoReceive(_query);
        }

        public static void DoReceive(string query, long? maxId = null)
        {
            var authInfo = AccountsStore.Accounts.Shuffle().Select(s => s.AuthenticateInfo).FirstOrDefault();
            if (authInfo == null)
            {
                BackpanelModel.RegisterEvent(new OperationFailedEvent("アカウントが登録されていないため、検索タイムラインを受信できませんでした。"));
                return;
            }
            authInfo.SearchTweets(query, max_id: maxId)
                .RegisterToStore();
        }
    }
}
