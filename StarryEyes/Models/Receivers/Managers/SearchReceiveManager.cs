using System.Collections.Generic;
using StarryEyes.Models.Receivers.ReceiveElements;

namespace StarryEyes.Models.Receivers.Managers
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
            lock (_searchLocker)
            {
                if (_searchReferenceCount.ContainsKey(query))
                {
                    _searchReferenceCount[query]++;
                }
                else
                {
                    _searchReferenceCount.Add(query, 1);
                    var receiver = new SearchReceiver(query);
                    _searchReceiverResolver.Add(query, receiver);
                }
            }
        }

        public void UnregisterSearchQuery(string query)
        {
            lock (_searchLocker)
            {
                if (!_searchReferenceCount.ContainsKey(query))
                    return;
                if (--_searchReferenceCount[query] != 0) return;
                _searchReferenceCount.Remove(query);
                var receiver = _searchReceiverResolver[query];
                _searchReceiverResolver.Remove(query);
                receiver.Dispose();
            }
        }
    }
}
