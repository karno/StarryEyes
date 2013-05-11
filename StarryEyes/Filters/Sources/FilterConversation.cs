using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;
using StarryEyes.Vanille.DataStore;

namespace StarryEyes.Filters.Sources
{
    public class FilterConversation : FilterSourceBase
    {
        private bool _initialized = false;
        private readonly long _original;
        private long _head;
        private readonly AVLTree<long> _statuses = new AVLTree<long>();

        public FilterConversation(string sid)
        {
            long id;
            if (!long.TryParse(sid, out id))
            {
                throw new ArgumentException("argument must be numeric value.");
            }
            this._original = id;
            this.FindHead(id)
                .ContinueWith(s => _head = s.Result)
                .ContinueWith(_ =>
                {
                    long[] array;
                    lock (_statuses)
                    {
                        array = _statuses.ToArray();
                    }
                    array.ToObservable()
                             .SelectMany(StoreHelper.GetTweet)
                             .Subscribe(s => StatusStore.Store(s));
                });
        }

        private async Task<long> FindHead(long id)
        {
            lock (_statuses)
            {
                _statuses.Add(id);
            }
            var replyTo = await Observable.Start(() => StoreHelper.GetTweet(id))
                                          .SelectMany(_ => _)
                                          .FirstOrDefaultAsync();
            if (replyTo == null || replyTo.InReplyToStatusId == null)
            {
                return id;
            }
            return await this.FindHead(replyTo.InReplyToStatusId.Value);
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
