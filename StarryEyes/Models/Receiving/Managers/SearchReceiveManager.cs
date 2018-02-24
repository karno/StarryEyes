using System.Collections.Generic;
using Cadena.Api.Parameters;
using Cadena.Engine.CyclicReceivers.Timelines;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receiving.Handling;

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
            lock (_searchLocker)
            {
                if (_searchReferenceCount.ContainsKey(query))
                {
                    _searchReferenceCount[query]++;
                }
                else
                {
                    var list = new List<ICollection<long>>();
                    _receiveCaches.Add(query, list);
                    _searchReferenceCount.Add(query, 1);
                    var receiver = new SearchReceiver(new RandomAccountAccessor(), StatusInbox.Enqueue,
                        BackstageModel.NotifyException, new SearchParameter(query, count: 100));
                    _searchReceiverResolver.Add(query, receiver);
                    ReceiveManager.ReceiveEngine.RegisterReceiver(receiver);
                }
            }
            lock (_receiveCaches[query])
            {
                _receiveCaches[query].Add(receiveCache);
            }
        }

        public void UnregisterSearchQuery(string query, ICollection<long> receiveCache)
        {
            lock (_searchLocker)
            {
                if (!_searchReferenceCount.ContainsKey(query))
                {
                    return;
                }
                lock (_receiveCaches[query])
                {
                    _receiveCaches[query].Remove(receiveCache);
                }
                if (--_searchReferenceCount[query] != 0)
                {
                    return;
                }
                _receiveCaches.Remove(query);
                _searchReferenceCount.Remove(query);
                var receiver = _searchReceiverResolver[query];
                _searchReceiverResolver.Remove(query);
                ReceiveManager.ReceiveEngine.UnregisterReceiver(receiver);
            }
        }
    }
}