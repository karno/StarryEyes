using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cadena.Data;
using JetBrains.Annotations;
using StarryEyes.Albireo.Collections;
using StarryEyes.Albireo.Helpers;
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
            cf?.BindingAccounts.ForEach(model.BindingAccounts.Add);
            return model;
        }

        private bool _isActivated;

        /// <summary>
        /// Set this tab is activated(connected stream, receive invalidate info, etc...)
        /// </summary>
        public bool IsActivated
        {
            get { return _isActivated; }
            set
            {
                if (value == _isActivated) return;
                _isActivated = value;
                IsSubscribeBroadcaster = value;
                if (_filterQuery == null) return;
                if (value)
                {
                    _filterQuery.Activate();
                    _filterQuery.InvalidateRequired += QueueInvalidateTimeline;
                    MuteBlockManager.RefreshTimelineRequired += QueueInvalidateTimeline;
                }
                else
                {
                    MuteBlockManager.RefreshTimelineRequired -= QueueInvalidateTimeline;
                    _filterQuery.InvalidateRequired -= QueueInvalidateTimeline;
                    _filterQuery.Deactivate();
                }
            }
        }

        private FilterQuery _filterQuery;

        /// <summary>
        /// Represents filter query. This property CAN BE NULL.
        /// </summary>
        public FilterQuery FilterQuery
        {
            get { return _filterQuery; }
            set
            {
                if ((value == null && _filterQuery == null) || (value != null && value.Equals(_filterQuery))) return;
                var old = _filterQuery;
                _filterQuery = value;
                _isQueryStringValid = false;
                QueueInvalidateTimeline();
                if (!_isActivated) return;
                if (_filterQuery != null)
                {
                    _filterQuery.Activate();
                    _filterQuery.InvalidateRequired += QueueInvalidateTimeline;
                }
                if (old != null)
                {
                    old.InvalidateRequired -= QueueInvalidateTimeline;
                    old.Deactivate();
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
            get { return String.IsNullOrWhiteSpace(_name) ? EmptyTabName : _name; }
            set { _name = value; }
        }

        [CanBeNull]
        public ICollection<long> BindingAccounts
        {
            get { return new NotifyCollection<long>(_bindingAccountIds, OnUpdateBoundAccounts); }
        }

        [CanBeNull]
        public IEnumerable<string> BindingHashtags
        {
            get { return _bindingHashtags ?? Enumerable.Empty<string>(); }
            set
            {
                if (value.SequenceEqual(_bindingHashtags)) return; // equal
                _bindingHashtags = value.Guard().ToList();
                TabManager.Save();
            }
        }

        private void OnUpdateBoundAccounts()
        {
            BindingAccountsChanged.SafeInvoke();
            TabManager.Save();
        }

        public bool NotifyNewArrivals { get; set; }

        public bool ShowUnreadCounts { get; set; }

        public string NotifySoundSource { get; set; }

        public string RawQueryString
        {
            get { return _rawQueryString; }
            set
            {
                _rawQueryString = value;
                _isQueryStringValid = false;
            }
        }

        public string GetQueryString()
        {
            // check raw query string is equals to filter query
            try
            {
                if (RawQueryString != null)
                {
                    if (!_isQueryStringValid &&
                        QueryCompiler.Compile(RawQueryString).ToQuery() == FilterQuery.ToQuery())
                    {
                        _isQueryStringValid = true;
                    }
                    return RawQueryString;
                }
            }
            catch
            {
            }
            RawQueryString = FilterQuery != null ? FilterQuery.ToQuery() : String.Empty;
            _isQueryStringValid = true;
            return RawQueryString;
        }

        #endregion Tab Parameters

        #region Filtering Control

        private string _filterSql = FilterExpressionBase.ContradictionSql;
        private Func<TwitterStatus, bool> _filterFunc = FilterExpressionBase.Contradiction;

        protected override bool PreInvalidateTimeline()
        {
            if (_filterQuery == null)
            {
                _filterSql = FilterExpressionBase.ContradictionSql;
                _filterFunc = FilterExpressionBase.Contradiction;
                return true;
            }
            _filterSql = _filterQuery.GetSqlQuery();
            _filterFunc = _filterQuery.GetEvaluator();
            return !_filterQuery.IsPreparing;
        }

        protected override bool CheckAcceptStatusCore(TwitterStatus status)
        {
            return _filterFunc(status);
        }

        #endregion Filtering Control

        #region Notification Chain

        public event Action<TwitterStatus> OnNewStatusArrival;

        protected override bool AddStatus(StatusModel model, bool isNewArrival)
        {
            var result = base.AddStatus(model, isNewArrival);
            if (result && isNewArrival)
            {
                OnNewStatusArrival.SafeInvoke(model.Status);
                if (NotifyNewArrivals)
                {
                    NotificationService.NotifyNewArrival(model.Status, this);
                }
            }
            return result;
        }

        #endregion Notification Chain

        protected override IObservable<TwitterStatus> Fetch(long? maxId, int? count)
        {
            return StatusProxy.FetchStatuses(_filterFunc, _filterSql, maxId, count)
                              .ToObservable()
                              .SelectMany(o => o)
                              .Merge(_filterQuery != null
                                  ? _filterQuery.ReceiveSources(maxId)
                                  : Observable.Empty<TwitterStatus>());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // dispose filter
            IsActivated = false;
        }
    }
}