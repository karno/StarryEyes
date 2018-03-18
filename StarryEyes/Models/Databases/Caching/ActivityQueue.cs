using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StarryEyes.Models.Databases.Caching
{
    public class ActivityQueue
    {
        private readonly int _threshold;
        private readonly TimeSpan _minInterval;
        private readonly object _intvlock = new object();
        private readonly Action<IEnumerable<Tuple<long, long>>> _addWriteback;
        private readonly Action<IEnumerable<Tuple<long, long>>> _removeWriteback;

        private readonly ConcurrentQueue<Tuple<long, long, bool>> _queue =
            new ConcurrentQueue<Tuple<long, long, bool>>();

        private DateTime _lastWritebackStamp;

        public ActivityQueue(int threshold, TimeSpan minInterval,
            Action<IEnumerable<Tuple<long, long>>> addWriteback,
            Action<IEnumerable<Tuple<long, long>>> removeWriteback)
        {
            _threshold = threshold;
            _minInterval = minInterval;
            _addWriteback = addWriteback;
            _removeWriteback = removeWriteback;
            _lastWritebackStamp = DateTime.Now;
        }

        public void GetDirtyActivity(long statusId,
            out IEnumerable<long> additions, out IEnumerable<long> deletions)
        {
            var adds = new List<long>();
            var removes = new List<long>();
            foreach (var tuple in _queue)
            {
                if (tuple.Item1 != statusId) continue;
                var item = tuple.Item2;
                if (tuple.Item3)
                {
                    // add
                    if (!adds.Contains(item))
                    {
                        adds.Add(item);
                    }
                    removes.Remove(item);
                }
                else
                {
                    // remove
                    if (!removes.Contains(item))
                    {
                        removes.Add(item);
                    }
                    adds.Remove(item);
                }
            }
            additions = adds;
            deletions = removes;
        }

        public void Add(long statusId, long userId)
        {
            _queue.Enqueue(Tuple.Create(statusId, userId, true));
            CheckNeedWriteback();
        }

        public void Remove(long statusId, long userId)
        {
            _queue.Enqueue(Tuple.Create(statusId, userId, false));
            CheckNeedWriteback();
        }

        private void CheckNeedWriteback()
        {
            if (_queue.Count <= _threshold) return;
            lock (_intvlock)
            {
                var span = DateTime.Now - _lastWritebackStamp;
                if (span < _minInterval) return;
                _lastWritebackStamp = DateTime.Now;
            }
            Task.Run(() => Writeback());
        }

        public void Writeback()
        {
            var adds = new List<Tuple<long, long>>();
            var removes = new List<Tuple<long, long>>();
            Tuple<long, long, bool> tuple;
            while (_queue.TryDequeue(out tuple))
            {
                var item = Tuple.Create(tuple.Item1, tuple.Item2);
                if (tuple.Item3)
                {
                    // add
                    if (!adds.Contains(item))
                    {
                        adds.Add(item);
                    }
                    removes.Remove(item);
                }
                else
                {
                    // remove
                    if (!removes.Contains(item))
                    {
                        removes.Add(item);
                    }
                    adds.Remove(item);
                }
            }
            _addWriteback(adds);
            _removeWriteback(removes);
        }
    }
}