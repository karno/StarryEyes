using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using JetBrains.Annotations;
using StarryEyes.Albireo.Collections;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Filters;
using StarryEyes.Filters.Expressions;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Timelines.Statuses;

namespace StarryEyes.Models.Timelines.Tabs
{
    public sealed class TabModel : TimelineModelBase
    {
        private const string EmptyTabName = "(untitled)";

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
                FilterQuery = query != null ? QueryCompiler.Compile(query) : null,
                RawQueryString = query,
                ShowUnreadCounts = true,
                NotifyNewArrivals = false,
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
                this.IsSubscribeBroadcaster = value;
                if (this._filterQuery == null) return;
                if (value)
                {
                    this._filterQuery.Activate();
                    this._filterQuery.InvalidateRequired += this.QueueInvalidateTimeline;
                    MuteBlockManager.RefreshTimelineRequired += this.QueueInvalidateTimeline;
                }
                else
                {
                    this._filterQuery.Deactivate();
                    this._filterQuery.InvalidateRequired -= this.QueueInvalidateTimeline;
                    MuteBlockManager.RefreshTimelineRequired -= this.QueueInvalidateTimeline;
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
                this._isQueryStringValid = false;
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

        private string _name;
        private string _rawQueryString;
        private bool _isQueryStringValid;
        private readonly AVLTree<long> _bindingAccountIds = new AVLTree<long>();
        private List<string> _bindingHashtags = new List<string>();

        public event Action BindingAccountsChanged;

        public string Name
        {
            get { return String.IsNullOrWhiteSpace(this._name) ? EmptyTabName : this._name; }
            set { this._name = value; }
        }

        [NotNull]
        public ICollection<long> BindingAccounts
        {
            get { return new NotifyCollection<long>(this._bindingAccountIds, this.OnUpdateBoundAccounts); }
        }

        [NotNull]
        public IEnumerable<string> BindingHashtags
        {
            get { return this._bindingHashtags ?? Enumerable.Empty<string>(); }
            set
            {
                if (value.SequenceEqual(this._bindingHashtags)) return; // equal
                this._bindingHashtags = value.Guard().ToList();
                TabManager.Save();
            }
        }

        private void OnUpdateBoundAccounts()
        {
            this.BindingAccountsChanged.SafeInvoke();
            TabManager.Save();
        }

        public bool NotifyNewArrivals { get; set; }

        public bool ShowUnreadCounts { get; set; }

        public string NotifySoundSource { get; set; }

        public string RawQueryString
        {
            get { return this._rawQueryString; }
            set
            {
                this._rawQueryString = value;
                this._isQueryStringValid = false;
            }
        }

        public string GetQueryString()
        {
            // check raw query string is equals to filter query
            try
            {
                if (RawQueryString != null)
                {
                    if (!this._isQueryStringValid &&
                        QueryCompiler.Compile(this.RawQueryString).ToQuery() == this.FilterQuery.ToQuery())
                    {
                        this._isQueryStringValid = true;
                    }
                    return RawQueryString;
                }
            }
            catch { }
            RawQueryString = this.FilterQuery != null ? this.FilterQuery.ToQuery() : String.Empty;
            this._isQueryStringValid = true;
            return RawQueryString;
        }

        #endregion

        #region Filtering Control

        private string _filterSql = FilterExpressionBase.ContradictionSql;
        private Func<TwitterStatus, bool> _filterFunc = FilterExpressionBase.Contradiction;

        protected override bool PreInvalidateTimeline()
        {
            if (this._filterQuery == null)
            {
                this._filterSql = FilterExpressionBase.ContradictionSql;
                this._filterFunc = FilterExpressionBase.Contradiction;
                return true;
            }
            this._filterSql = this._filterQuery.GetSqlQuery();
            this._filterFunc = this._filterQuery.GetEvaluator();
            return !this._filterQuery.IsPreparing;
        }

        protected override bool CheckAcceptStatusCore(TwitterStatus status)
        {
            return this._filterFunc(status);
        }

        #endregion

        #region Notification Chain

        public event Action<TwitterStatus> OnNewStatusArrival;

        protected override bool AddStatus(StatusModel model, bool isNewArrival)
        {
            var result = base.AddStatus(model, isNewArrival);
            if (result && isNewArrival)
            {
                this.OnNewStatusArrival.SafeInvoke(model.Status);
                if (this.NotifyNewArrivals)
                {
                    NotificationService.NotifyNewArrival(model.Status, this);
                }
            }
            return result;
        }

        #endregion

        protected override IObservable<TwitterStatus> Fetch(long? maxId, int? count)
        {
            return StatusProxy.FetchStatuses(this._filterFunc, this._filterSql, maxId, count)
                              .ToObservable()
                              .Merge(_filterQuery != null
                                  ? _filterQuery.ReceiveSources(maxId)
                                  : Observable.Empty<TwitterStatus>());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // dispose filter
            this.IsActivated = false;
        }
    }
}
