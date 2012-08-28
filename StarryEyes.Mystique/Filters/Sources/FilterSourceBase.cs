using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Mystique.Filters.Expressions.Operators;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.Mystique.Settings;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Sources
{
    /// <summary>
    /// Tweets source of status
    /// </summary>
    public abstract class FilterSourceBase
    {
        public abstract string FilterKey { get; }

        public abstract string FilterValue { get; }

        public abstract Func<TwitterStatus, bool> GetEvaluator();

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
        /// <param name="max_id">receiving threshold id</param>
        public IObservable<TwitterStatus> Receive(long? max_id)
        {
            return ReceiveSink(max_id)
                .Do(_ => StatusStore.Store(_));
        }

        protected virtual IObservable<TwitterStatus> ReceiveSink(long? max_id)
        {
            return Observable.Empty<TwitterStatus>();
        }

        /// <summary>
        /// Get accounts from screen name.
        /// </summary>
        /// <param name="screenName">partial screen name</param>
        /// <returns>accounts collection</returns>
        protected IEnumerable<AuthenticateInfo> GetAccountsFromString(string screenName)
        {
            if (String.IsNullOrEmpty(screenName))
                return Setting.Accounts.Select(i => i.AuthenticateInfo);
            else
                return Setting.Accounts
                    .Select(i => i.AuthenticateInfo)
                    .Where(i => FilterOperatorEquals.StringMatch(i.UnreliableScreenName, screenName,
                        FilterOperatorEquals.StringArgumentSide.Right));
        }
    }
}
