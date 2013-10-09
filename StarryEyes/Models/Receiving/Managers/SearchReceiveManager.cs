using System.Collections.Generic;
using StarryEyes.Models.Receiving.Receivers;

namespace StarryEyes.Models.Receiving.Managers
{
    internal class SearchReceiveManager
    {
        private readonly object _searchLocker = new object();
        private readonly IDictionary<string, SearchReceiver> _searchReceiverResolver
            = new Dictionary<string, SearchReceiver>();
        private readonly IDictionary<string, int> _searchReferenceCount
            = new Dictionary<string, int>();

        public void RegisterSearchQuery(string query)
        {
            lock (this._searchLocker)
            {
                if (this._searchReferenceCount.ContainsKey(query))
                {
                    this._searchReferenceCount[query]++;
                }
                else
                {
                    this._searchReferenceCount.Add(query, 1);
                    var receiver = new SearchReceiver(query);
                    this._searchReceiverResolver.Add(query, receiver);
                }
            }
        }

        public void UnregisterSearchQuery(string query)
        {
            lock (this._searchLocker)
            {
                if (!this._searchReferenceCount.ContainsKey(query))
                    return;
                if (--this._searchReferenceCount[query] != 0) return;
                this._searchReferenceCount.Remove(query);
                var receiver = this._searchReceiverResolver[query];
                this._searchReceiverResolver.Remove(query);
                receiver.Dispose();
            }
        }
    }
}
