using System.Collections.Generic;
using StarryEyes.Models.Receiving.Receivers;

namespace StarryEyes.Models.Receiving.Managers
{
    internal class SearchReceiveManager
    {
        private readonly object _searchLocker = new object();

        private readonly IDictionary<string, SearchReceiver> _searchReceiverResolver =
            new Dictionary<string, SearchReceiver>();

        private readonly IDictionary<string, List<ICollection<long>>> _receiveCaches =
            new Dictionary<string, List<ICollection<long>>>();

        private readonly IDictionary<string, int> _searchReferenceCount =
            new Dictionary<string, int>();

        public void RegisterSearchQuery(string query, ICollection<long> receiveCache)
        {
            lock (this._searchLocker)
            {
                if (this._searchReferenceCount.ContainsKey(query))
                {
                    this._searchReferenceCount[query]++;
                }
                else
                {
                    var list = new List<ICollection<long>>();
                    this._receiveCaches.Add(query, list);
                    this._searchReferenceCount.Add(query, 1);
                    var receiver = new SearchReceiver(query, list);
                    this._searchReceiverResolver.Add(query, receiver);
                }
            }
            lock (this._receiveCaches[query])
            {
                this._receiveCaches[query].Add(receiveCache);
            }
        }

        public void UnregisterSearchQuery(string query, ICollection<long> receiveCache)
        {
            lock (this._searchLocker)
            {
                if (!this._searchReferenceCount.ContainsKey(query))
                {
                    return;
                }
                lock (_receiveCaches[query])
                {
                    this._receiveCaches[query].Remove(receiveCache);
                }
                if (--this._searchReferenceCount[query] != 0)
                {
                    return;
                }
                this._receiveCaches.Remove(query);
                this._searchReferenceCount.Remove(query);
                var receiver = this._searchReceiverResolver[query];
                this._searchReceiverResolver.Remove(query);
                receiver.Dispose();
            }
        }
    }
}
