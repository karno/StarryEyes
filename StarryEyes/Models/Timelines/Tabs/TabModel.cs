using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo.Collections;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.Models.Timelines.Tabs
{
    public sealed class TabModel : TimelineModelBase
    {
        /// <summary>
        /// Create new tab model
        /// </summary>
        /// <param name="name">tab name</param>
        /// <param name="query">tab query(KQ)</param>
        /// <returns>tab model</returns>
        public static TabModel Create(string name, string query)
        {
            var model = new TabModel
            {
                Name = name,
                FilterQuery = QueryCompiler.Compile(query),
                IsShowUnreadCounts = true,
                IsNotifyNewArrivals = true
            };
            var cf = TabManager.CurrentFocusTab;
            if (cf != null)
            {
                cf.BindingAccounts.ForEach(model.BindingAccounts.Add);
            }
            return model;
        }

        private bool _isActivated;
        /// <summary>
        /// Set this tab is activated(connected stream, receive invalidate info, etc...)
        /// </summary>
        public bool IsActivated
        {
            get { return this._isActivated; }
            set
            {
                if (value == this._isActivated) return;
                this._isActivated = value;
                this.QueueInvalidateTimeline();
                this.IsSubscribeBroadcaster = value;
                if (this._filterQuery == null) return;
                if (value)
                {
                    this._filterQuery.Activate();
                    this._filterQuery.InvalidateRequired += this.QueueInvalidateTimeline;
                }
                else
                {
                    this._filterQuery.Deactivate();
                    this._filterQuery.InvalidateRequired -= this.QueueInvalidateTimeline;
                }
            }
        }

        private FilterQuery _filterQuery;
        /// <summary>
        /// Represents filter query. This property CAN BE NULL.
        /// </summary>
        public FilterQuery FilterQuery
        {
            get { return this._filterQuery; }
            set
            {
                if ((value == null && _filterQuery == null) || (value != null && value.Equals(_filterQuery))) return;
                var old = this._filterQuery;
                this._filterQuery = value;
                this.QueueInvalidateTimeline();
                if (!this._isActivated) return;
                if (this._filterQuery != null)
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

        #region Tab Parameters

        private readonly AVLTree<long> _bindingAccountIds = new AVLTree<long>();
        private List<string> _bindingHashtags = new List<string>();

        public event Action BindingAccountsChanged;

        public string Name { get; set; }

        public ICollection<long> BindingAccounts
        {
            get { return new NotifyCollection<long>(this._bindingAccountIds, this.OnUpdateBoundAccounts); }
        }

        public IEnumerable<string> BindingHashtags
        {
            get { return this._bindingHashtags ?? Enumerable.Empty<string>(); }
            set
            {
                this._bindingHashtags = value.Guard().ToList();
                TabManager.Save();
            }
        }

        private void OnUpdateBoundAccounts()
        {
            var handler = this.BindingAccountsChanged;
            if (handler != null) handler();
            TabManager.Save();
        }

        public bool IsNotifyNewArrivals { get; set; }

        public bool IsShowUnreadCounts { get; set; }

        public string NotifySoundSource { get; set; }

        #endregion

        #region Filtering Control

        private string _filterSql = FilterExpressionBase.ContradictionSql;
        private Func<TwitterStatus, bool> _filterFunc = FilterExpressionBase.Contradiction;

        protected override void PreInvalidateTimeline()
        {
            if (this._filterQuery == null)
            {
                this._filterSql = FilterExpressionBase.ContradictionSql;
                this._filterFunc = FilterExpressionBase.Contradiction;
            }
            else
            {
                this._filterSql = this._filterQuery.GetSqlQuery();
                this._filterFunc = this._filterQuery.GetEvaluator();
            }
        }

        protected override bool CheckAcceptStatus(TwitterStatus status)
        {
            return this._filterFunc(status);
        }

        #endregion

        #region Notification Chain

        public event Action<TwitterStatus> OnNewStatusArrival;

        protected override async Task<bool> AddStatus(TwitterStatus status, bool isNewArrival)
        {
            var result = await base.AddStatus(status, isNewArrival);
            if (result)
            {
                OnNewStatusArrival(status);
                if (this.IsNotifyNewArrivals)
                {
                    NotificationService.NotifyNewArrival(status, this);
                }
            }
            return result;
        }

        #endregion

        protected override IObservable<TwitterStatus> Fetch(long? maxId, int? count)
        {
            return StatusProxy.FetchStatuses(this._filterSql, maxId, count);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // dispose filter
            this.IsActivated = false;
        }
    }
}
