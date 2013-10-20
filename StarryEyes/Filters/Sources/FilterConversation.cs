using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo.Collections;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Databases;

namespace StarryEyes.Filters.Sources
{
    public class FilterConversation : FilterSourceBase
    {
        private readonly long _original;
        private readonly AVLTree<long> _statuses = new AVLTree<long>();

        private bool _isPreparing;
        public override bool IsPreparing
        {
            get { return _isPreparing; }
        }

        public FilterConversation(string sid)
        {
            long id;
            if (!long.TryParse(sid, out id))
            {
                throw new ArgumentException("argument must be numeric value.");
            }
            this._original = id;
            _statuses.Add(id);
        }

        public override void Activate()
        {
            base.Activate();
            this.TraceBackConversations(this._original);
        }

        private async void TraceBackConversations(long origin)
        {
            try
            {
                _isPreparing = true;
                var queue = new Queue<long>();
                var list = new List<long>();
                queue.Enqueue(await this.FindHeadAsync(origin));
                while (queue.Count > 0)
                {
                    var cid = queue.Dequeue();
                    list.Add(cid);
                    var ids = await StatusProxy.FindFromInReplyToAsync(cid);
                    ids.ForEach(queue.Enqueue);
                }
                lock (_statuses)
                {
                    list.ForEach(_statuses.Add);
                }
            }
            finally
            {
                _isPreparing = false;
                this.RaiseInvalidateRequired();
            }
        }

        private async Task<long> FindHeadAsync(long id)
        {
            var irt = await StatusProxy.GetInReplyToAsync(id);
            if (irt == null) return id;
            return await this.FindHeadAsync(irt.Value);
        }

        public override string FilterKey
        {
            get { return "talk:"; }
        }

        public override string FilterValue
        {
            get { return this._original.ToString(); }
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return this.CheckConversation;
        }

        public override string GetSqlQuery()
        {
            long[] ids;
            lock (_statuses)
            {
                ids = _statuses.ToArray();
            }
            System.Diagnostics.Debug.WriteLine("sql: " + "Id IN (" + ids.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",") + ")");
            return "Id IN (" + ids.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",") + ")";
        }

        private bool CheckConversation(TwitterStatus status)
        {
            lock (_statuses)
            {
                if (_statuses.Contains(status.Id))
                {
                    return true;
                }
                if (status.InReplyToStatusId == null) return false;
                if (_statuses.Contains(status.InReplyToStatusId.Value))
                {
                    _statuses.Add(status.Id);
                    return true;
                }
            }
            return false;
        }
    }
}
