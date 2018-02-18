using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Globalization.Filters;
using StarryEyes.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Sources
{
    /// <summary>
    /// Tweets source of status
    /// </summary>
    public abstract class FilterSourceBase
    {
        public abstract string FilterKey { get; }

        public abstract string FilterValue { get; }

        public abstract Func<TwitterStatus, bool> GetEvaluator();

        public virtual bool IsPreparing { get { return false; } }

        public abstract string GetSqlQuery();

        public event Action InvalidateRequired;

        protected virtual void RaiseInvalidateRequired()
        {
            this.InvalidateRequired.SafeInvoke();
        }

        /// <summary>
        /// Activate dependency receiving method.
        /// </summary>
        public virtual void Activate() { }

        /// <summary>
        /// Deactivate dependency receiving method.
        /// </summary>
        public virtual void Deactivate() { }

        /// <summary>
        /// Receive older tweets. <para />
        /// Tweets are registered to StatusStore automatically.
        /// </summary>
        /// <param name="maxId">receiving threshold id</param>
        public IObservable<TwitterStatus> Receive(long? maxId)
        {
            return ReceiveSink(maxId)
                .Do(StatusInbox.Enqueue)
                .Catch((Exception ex) =>
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent(
                        FilterObjectResources.FilterSourceBaseReceiveFailed +
                        " " + FilterKey + ": " + FilterValue, ex));
                    return Observable.Empty<TwitterStatus>();
                });
        }

        protected virtual IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return Observable.Empty<TwitterStatus>();
        }

        /// <summary>
        /// Get accounts from screen name.
        /// </summary>
        /// <param name="screenName">partial screen name</param>
        /// <returns>accounts collection</returns>
        protected IEnumerable<TwitterAccount> GetAccountsFromString(string screenName)
        {
            if (String.IsNullOrEmpty(screenName))
            {
                return Setting.Accounts.Collection.ToArray();
            }
            // *kar => unkar
            // kar* => karno
            // *kar* => dakara
            var filtered =
                new string(screenName.Where(c =>
                                            (c >= 'A' && c <= 'Z') ||
                                            (c >= 'a' && c <= 'z') ||
                                            (c >= '0' && c <= '9') ||
                                            c == '_' || c == '*')
                                     .ToArray());
            var pattern = filtered.Replace("*", ".*");
            return Setting.Accounts.Collection
                          .Where(i => Regex.IsMatch(i.UnreliableScreenName, pattern));
        }
    }
}
