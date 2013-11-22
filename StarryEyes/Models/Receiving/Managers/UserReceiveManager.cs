using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receiving.Receivers;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Managers
{
    internal sealed class UserReceiveManager
    {
        private readonly object _bundlesLocker = new object();

        private readonly IDictionary<long, UserReceiveBundle> _bundles =
            new Dictionary<long, UserReceiveBundle>();

        public event Action TrackRearranged;

        public event Action<TwitterAccount> ConnectionStateChanged;

        public UserStreamsConnectionState GetConnectionState(long id)
        {
            UserReceiveBundle bundle;
            return !this._bundles.TryGetValue(id, out bundle)
                       ? UserStreamsConnectionState.Invalid
                       : bundle.ConnectionState;
        }

        public IKeywordTrackable GetSuitableKeywordTracker()
        {
            lock (this._bundlesLocker)
            {
                return this._bundles.Values
                               .Where(c => c.IsUserStreamsEnabled)
                               .Where(c => c.TrackKeywords.Count() < UserStreamsReceiver.MaxTrackingKeywordCounts)
                               .OrderBy(c => c.TrackKeywords.Count())
                               .FirstOrDefault();
            }
        }

        public IKeywordTrackable GetKeywordTrackerFromId(long id)
        {
            lock (this._bundlesLocker)
            {
                UserReceiveBundle ret;
                return this._bundles.TryGetValue(id, out ret) ? ret : null;
            }
        }

        public IEnumerable<IKeywordTrackable> GetTrackers()
        {
            lock (this._bundlesLocker)
            {
                return this._bundles.Values.ToArray();
            }
        }

        public UserReceiveManager()
        {
            System.Diagnostics.Debug.WriteLine("UserReceiveManager initialized.");
            Setting.Accounts.Collection.ListenCollectionChanged()
                   .Subscribe(_ => Task.Run(() => this.NotifySettingChanged()));
            App.UserInterfaceReady += this.NotifySettingChanged;
        }

        // ReSharper disable AccessToModifiedClosure
        private void NotifySettingChanged()
        {
            var accounts = Setting.Accounts.Collection.ToDictionary(a => a.Id);
            var danglings = new List<string>();
            var rearranged = false;
            lock (this._bundlesLocker)
            {
                // remove deauthroized accounts
                this._bundles.Values
                    .Where(s => !accounts.ContainsKey(s.UserId))
                    .ToArray()
                    .Do(b => this._bundles.Remove(b.UserId))
                    .Do(b => danglings.AddRange(b.TrackKeywords))
                    .ForEach(c => c.Dispose());

                // add new users
                accounts.Where(s => !this._bundles.ContainsKey(s.Key))
                        .Select(s => new UserReceiveBundle(s.Value))
                        .Do(s => s.StateChanged += arg => this.ConnectionStateChanged.SafeInvoke(arg))
                        .ForEach(b => this._bundles.Add(b.UserId, b));

                // stop cancelled streamings
                this._bundles.Values
                        .Where(b => b.IsUserStreamsEnabled && !accounts[b.UserId].IsUserStreamsEnabled)
                        .Do(b => danglings.AddRange(b.TrackKeywords))
                        .Do(b => b.IsUserStreamsEnabled = false)
                        .ForEach(b => b.TrackKeywords = null);

                if (danglings.Count > 0)
                {
                    rearranged = true;
                }

                // start new streamings
                this._bundles.Values
                    .Where(b => !b.IsUserStreamsEnabled && accounts[b.UserId].IsUserStreamsEnabled)
                    .ForEach(c =>
                    {
                        c.TrackKeywords = danglings.Take(UserStreamsReceiver.MaxTrackingKeywordCounts);
                        c.IsUserStreamsEnabled = true;
                        danglings = danglings.Skip(UserStreamsReceiver.MaxTrackingKeywordCounts).ToList();
                    });

                while (danglings.Count > 0)
                {
                    var bundle = this._bundles.Values
                            .Where(b => b.IsUserStreamsEnabled)
                            .OrderBy(b => b.TrackKeywords.Count())
                            .FirstOrDefault();
                    if (bundle == null) break;
                    var keywordCount = bundle.TrackKeywords.Count();
                    if (keywordCount >= UserStreamsReceiver.MaxTrackingKeywordCounts) break;
                    var assignable = UserStreamsReceiver.MaxTrackingKeywordCounts - keywordCount;
                    bundle.TrackKeywords = bundle.TrackKeywords.Concat(danglings.Take(assignable)).ToArray();
                    danglings = danglings.Skip(assignable).ToList();
                }
            }
            if (!rearranged) return;
            TrackRearranged.SafeInvoke();
        }
        // ReSharper restore AccessToModifiedClosure

        public void ReconnectStream(long id)
        {
            UserReceiveBundle bundle;
            lock (this._bundlesLocker)
            {
                this._bundles.TryGetValue(id, out bundle);
            }
            if (bundle != null)
            {
                bundle.ReconnectUserStreams();
            }
        }

        public void ReconnectAllStreams()
        {
            lock (this._bundlesLocker)
            {
                this._bundles.Values.ForEach(c => c.ReconnectUserStreams());
            }
        }

        private sealed class UserReceiveBundle : IDisposable, IKeywordTrackable
        {
            private readonly TwitterAccount _account;
            private readonly CompositeDisposable _disposable;
            private readonly UserStreamsReceiver _userStreamsReceiver;

            public event Action<TwitterAccount> StateChanged;

            public IEnumerable<string> TrackKeywords
            {
                get { return this._userStreamsReceiver.TrackKeywords; }
                set { this._userStreamsReceiver.TrackKeywords = value; }
            }

            public long UserId { get { return this._account.Id; } }

            public UserReceiveBundle(TwitterAccount account)
            {
                this._account = account;
                this._disposable = new CompositeDisposable
                {
                    (this._userStreamsReceiver = new UserStreamsReceiver(account)),
                    new HomeTimelineReceiver(account),
                    new MentionTimelineReceiver(account),
                    new DirectMessagesReceiver(account),
                    new UserInfoReceiver(account),
                    new UserTimelineReceiver(account),
                    new UserRelationReceiver(account)
                };
                this._userStreamsReceiver.StateChanged += () => StateChanged.SafeInvoke(account);
            }

            public bool IsUserStreamsEnabled
            {
                get { return this._userStreamsReceiver.IsEnabled; }
                set { this._userStreamsReceiver.IsEnabled = value; }
            }

            public UserStreamsConnectionState ConnectionState
            {
                get
                {
                    return this._userStreamsReceiver == null
                               ? UserStreamsConnectionState.Invalid
                               : this._userStreamsReceiver.ConnectionState;
                }
            }

            public void Dispose()
            {
                this._disposable.Dispose();
            }

            public void ReconnectUserStreams()
            {
                this._userStreamsReceiver.Reconnect();
            }
        }
    }

    internal interface IKeywordTrackable
    {
        IEnumerable<string> TrackKeywords { get; set; }

        long UserId { get; }
    }
}
