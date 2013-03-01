using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;
using StarryEyes.Vanille.DataStore;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlip
{
    public class SearchResultViewModel : TimelineViewModelBase
    {
        private readonly TimelineModel _timelineModel;

        private readonly Func<TwitterStatus, bool> _predicate;

        public SearchResultViewModel(string query)
        {
            _timelineModel = new TimelineModel(
                _predicate = CreatePredicate(query),
                (id, c, _) => Fetch(query, id, c));
        }

        private Func<TwitterStatus, bool> CreatePredicate(string query)
        {
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
            if (Setting.IsSearchFromWeb.Value)
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
}
