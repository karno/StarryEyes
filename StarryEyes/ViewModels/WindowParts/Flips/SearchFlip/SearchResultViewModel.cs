using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Tab;
using StarryEyes.Vanille.DataStore;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlip
{
    public class SearchResultViewModel : TimelineViewModelBase
    {
        private readonly SearchOption _option;
        private readonly TimelineModel _timelineModel;

        private readonly Func<TwitterStatus, bool> _predicate;
        private FilterQuery _localQuery;

        public SearchResultViewModel(string query, SearchOption option)
        {
            _option = option;
            _timelineModel = new TimelineModel(
                _predicate = CreatePredicate(query, option),
                (id, c, _) => Fetch(query, id, c));
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

        private IObservable<TwitterStatus> Fetch(string query, long? maxId, int count)
        {
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
            if (_option == SearchOption.Web)
            {
                var info = AccountsStore.Accounts
                                        .Shuffle()
                                        .Select(s => s.AuthenticateInfo)
                                        .FirstOrDefault();
                if (info == null)
                    return Observable.Empty<TwitterStatus>();
                return fetch.Merge(info.SearchTweets(query, count: count, max_id: maxId)
                                       .Catch((Exception ex) =>
                                       {
                                           BackpanelModel.RegisterEvent(new OperationFailedEvent(ex.Message));
                                           return Observable.Empty<TwitterStatus>();
                                       }));
            }
            return fetch;
        }

        protected override TimelineModel TimelineModel
        {
            get { return _timelineModel; }
        }

        protected override IEnumerable<long> CurrentAccounts
        {
            get
            {
                var ctab = MainAreaModel.CurrentFocusTab;
                return ctab != null ? ctab.BindingAccountIds : Enumerable.Empty<long>();
            }
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
        /// Search local store by query.
        /// </summary>
        Query,
        /// <summary>
        /// Search on web by keyword.
        /// </summary>
        Web
    }
}
