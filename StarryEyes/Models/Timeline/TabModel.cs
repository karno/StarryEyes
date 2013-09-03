using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters;
using StarryEyes.Filters.Expressions;
using StarryEyes.Models.Stores;
using StarryEyes.Vanille.DataStore;

namespace StarryEyes.Models.Timeline
{
    public sealed class TabModel : TimelineModelBase
    {
        private bool _isActivated;
        public bool IsActivated
        {
            get { return _isActivated; }
            set
            {
                if (value == _isActivated) return;
                _isActivated = value;
                this.QueueInvalidateTimeline();
                IsGlobalReceiverListening = value;
                if (_filterQuery == null) return;
                if (value)
                {
                    _filterQuery.Activate();
                    _filterQuery.InvalidateRequired += this.QueueInvalidateTimeline;
                }
                else
                {
                    _filterQuery.Deactivate();
                    _filterQuery.InvalidateRequired -= this.QueueInvalidateTimeline;
                }
            }
        }

        private FilterQuery _filterQuery;
        public FilterQuery FilterQuery
        {
            get { return _filterQuery; }
            set
            {
                if (value == _filterQuery) return;
                var old = _filterQuery;
                _filterQuery = value;
                this.QueueInvalidateTimeline();
                if (!this._isActivated) return;
                if (_filterQuery != null)
                {
                    this._filterQuery.Activate();
                    this._filterQuery.InvalidateRequired += this.QueueInvalidateTimeline;
                }
                if (old != null)
                {
                    old.Deactivate();
                    old.InvalidateRequired -= this.QueueInvalidateTimeline;
                }
            }
        }

        #region Filtering Control

        private string _filterSql = FilterExpressionBase.ContradictionSql;
        private Func<TwitterStatus, bool> _filterFunc = FilterExpressionBase.Contradiction;

        protected override void InvalidateCache()
        {
            if (_filterQuery == null)
            {
                _filterSql = FilterExpressionBase.ContradictionSql;
                _filterFunc = FilterExpressionBase.Contradiction;
            }
            else
            {
                _filterSql = FilterQuery.GetSqlQuery();
                _filterFunc = FilterQuery.GetEvaluator();
            }
        }

        protected override bool CheckAcceptStatus(TwitterStatus status)
        {
            return _filterFunc(status);
        }

        #endregion

        protected override IObservable<TwitterStatus> Fetch(long? maxId, int? count)
        {
            return StatusStore.Find(_filterFunc,
                                    maxId != null ? FindRange<long>.By(maxId.Value) : null,
                                    count);
        }

        protected override async Task<IEnumerable<TwitterStatus>> FetchBatch(long? maxId, int? count)
        {
            var list = new List<TwitterStatus>();
            await StatusStore.FindBatch(_filterFunc, count ?? 128)
                             .ForEachAsync(list.Add);
            return list;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // dispose filter
            IsActivated = false;
        }
    }
}
