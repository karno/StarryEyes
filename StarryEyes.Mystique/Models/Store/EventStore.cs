using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Models.Store
{
    /// <summary>
    /// Manage and store event notifications from Twitter.<para />
    /// (store Favorites, Retweets, and Follows)
    /// </summary>
    public static class EventStore
    {
        #region publish block

        private static Subject<TwitterStreamingElement> eventPublisher = new Subject<TwitterStreamingElement>();

        public static IObservable<TwitterStreamingElement> EventPublisher
        {
            get { return eventPublisher; }
        }

        #endregion

        // イベントどうしよう。

        internal static void Store(TwitterStreamingElement elem)
        {
            eventPublisher.OnNext(elem);
        }
    }

    public class StoredTwitterStreamingElement
    {
        public StoredTwitterStreamingElement() { }

        public StoredTwitterStreamingElement(TwitterStreamingElement elem)
        {
            this.EventType = elem.EventType;
            this.EventSourceUserId = elem.EventSourceUser.Id;
            this.EventTargetUserId = elem.EventTargetUser.Id;
            this.EventTargetStatusId = elem.EventTargetTweet != null ? (long?)elem.EventTargetTweet.Id : null;
            this.EventCreatedAt = elem.EventCreatedAt;
        }

        public EventType EventType { get; set; }

        public long EventSourceUserId { get; set; }

        public long EventTargetUserId { get; set; }

        public long? EventTargetStatusId { get; set; }

        public DateTime EventCreatedAt { get; set; }
    }
}
