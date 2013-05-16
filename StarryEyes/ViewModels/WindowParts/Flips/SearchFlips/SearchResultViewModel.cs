using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Livet.Messaging;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Tab;
using StarryEyes.Vanille.DataStore;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class SearchResultViewModel : TimelineViewModelBase
    {
        private readonly SearchFlipViewModel _parent;

        public SearchFlipViewModel Parent
        {
            get { return this._parent; }
        }

        private readonly string _query;
        public string Query
        {
            get { return _query; }
        }

        private readonly SearchOption _option;
        private readonly TimelineModel _timelineModel;

        private readonly Func<TwitterStatus, bool> _predicate;
        private FilterQuery _localQuery;

        public SearchResultViewModel(SearchFlipViewModel parent, string query, SearchOption option)
        {
            this._parent = parent;
            _query = query;
            _option = option;
            _timelineModel = new TimelineModel(
                _predicate = CreatePredicate(query, option),
                (id, c, _) => Fetch(id, c));
            this.CompositeDisposable.Add(_timelineModel);
            IsLoading = true;
            _timelineModel.ReadMore(null)
                          .Finally(() => IsLoading = false)
                          .Subscribe();
            MainAreaViewModel.TimelineActionTargetOverride = this;
        }

        private Func<TwitterStatus, bool> CreatePredicate(string query, SearchOption option)
        {
            if (option == SearchOption.Query)
            {
                try
                {
                    return (_localQuery = QueryCompiler.Compile(query)).GetEvaluator();
                }
                catch
                {
                    return _ => false;
                }
            }
            var splitted = query.Split(new[] { " ", "\t", "　" },
                                       StringSplitOptions.RemoveEmptyEntries)
                                .Distinct().ToArray();
            var positive = splitted.Where(s => !s.StartsWith("-")).ToArray();
            var negative = splitted.Where(s => s.StartsWith("-")).Select(s => s.Substring(1)).ToArray();
            return status =>
                   positive.Any(s => status.Text.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0) &&
                   !negative.Any(s => status.Text.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        private IObservable<TwitterStatus> Fetch(long? maxId, int count)
        {
            switch (_option)
            {
                case SearchOption.Quick:
                    if (TabManager.CurrentFocusTab == null)
                    {
                        return Observable.Empty<TwitterStatus>();
                    }
                    return Observable.Start(() => TabManager.CurrentFocusTab.Timeline.Statuses)
                                     .SelectMany(s => s)
                                     .Select(s => s.Status)
                                     .Where(s => _predicate(s));
                case SearchOption.Web:
                    var info = AccountsStore.Accounts
                                            .Shuffle()
                                            .Select(s => s.AuthenticateInfo)
                                            .FirstOrDefault();
                    if (info == null)
                    {
                        return Observable.Empty<TwitterStatus>();
                    }
                    return info.SearchTweets(_query, count: count, max_id: maxId)
                               .Catch((Exception ex) =>
                               {
                                   BackstageModel.RegisterEvent(new OperationFailedEvent(ex.Message));
                                   return Observable.Empty<TwitterStatus>();
                               });
                default:
                    var fetch = Observable.Start(() =>
                                                 StatusStore.Find(_predicate,
                                                                  maxId != null
                                                                      ? FindRange<long>.By(maxId.Value)
                                                                      : null,
                                                                  count))
                                          .SelectMany(_ => _);
                    if (_localQuery != null && _localQuery.Sources != null)
                    {
                        fetch = fetch.Merge(
                            _localQuery.Sources.ToObservable().SelectMany(s => s.Receive(maxId)));
                    }
                    return fetch;
            }
        }

        protected override TimelineModel TimelineModel
        {
            get { return _timelineModel; }
        }

        protected override IEnumerable<long> CurrentAccounts
        {
            get
            {
                var ctab = TabManager.CurrentFocusTab;
                return ctab != null ? ctab.BindingAccountIds : Enumerable.Empty<long>();
            }
        }

        public void SetPhysicalFocus()
        {
            this.Messenger.Raise(new InteractionMessage("SetPhysicalFocus"));
        }

        public override void GotPhysicalFocus()
        {
            MainAreaViewModel.TimelineActionTargetOverride = this;
        }

        public void Close()
        {
            MainAreaViewModel.TimelineActionTargetOverride = null;
            this.Parent.RewindStack();
        }

        public void PinToTab()
        {
            TabManager.CreateTab(
                new TabModel(Query, this.CreateFilterQuery(Query, _option)));
            Close();
        }

        private string CreateFilterQuery(string query, SearchOption option)
        {
            if (option == SearchOption.Query)
            {
                try
                {
                    QueryCompiler.Compile(query).GetEvaluator();
                    return _query;
                }
                catch
                {
                    return CommonTabBuilder.Empty;
                }
            }
            var splitted = query.Split(new[] { " ", "\t", "　" },
                                       StringSplitOptions.RemoveEmptyEntries)
                                .Distinct().ToArray();
            var positive = splitted.Where(s => !s.StartsWith("-")).ToArray();
            var negative = splitted.Where(s => s.StartsWith("-")).Select(s => s.Substring(1)).ToArray();
            return
                positive.Select(s => "text == \"" + s + "\"")
                        .Concat(negative.Select(s => "text != \"" + s + "\""))
                        .JoinString("&&");
        }

    }

    /// <summary>
    /// Describes searching option.
    /// </summary>
    public enum SearchOption
    {
        /// <summary>
        /// Search local store by keyword.
        /// </summary>
        None,
        /// <summary>
        /// Search from tabs only
        /// </summary>
        Quick,
        /// <summary>
        /// Search local store by query.
        /// </summary>
        Query,
        /// <summary>
        /// Search on web by keyword.
        /// </summary>
        Web
    }
}
