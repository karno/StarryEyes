using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receivers.ReceiveElements;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receivers.Managers
{
    internal class UserReceiveManager
    {
        private readonly object _bundlesLocker = new object();

        private readonly SortedDictionary<long, UserReceiveBundle> _bundles =
            new SortedDictionary<long, UserReceiveBundle>();

        public event Action TrackRearranged;

        public event Action<long> ConnectionStateChanged;

        protected virtual void OnConnectionStateChanged(long obj)
        {
            var handler = this.ConnectionStateChanged;
            if (handler != null) handler(obj);
        }

        public UserStreamsConnectionState GetConnectionState(long id)
        {
            UserReceiveBundle bundle;
            return !this._bundles.TryGetValue(id, out bundle)
                       ? UserStreamsConnectionState.Invalid
                       : bundle.ConnectionState;
        }

        public IKeywordTrackable GetSuitableKeywordTracker()
        {
            lock (_bundlesLocker)
            {
                return _bundles.Values
                               .Where(c => c.IsUserStreamsEnabled)
                               .Where(c => c.TrackKeywords.Count() < UserStreamsReceiver.MaxTrackingKeywordCounts)
                               .OrderBy(c => c.TrackKeywords.Count())
                               .FirstOrDefault();
            }
        }

        public IKeywordTrackable GetKeywordTrackerFromId(long id)
        {
            lock (_bundlesLocker)
            {
                UserReceiveBundle ret;
                return _bundles.TryGetValue(id, out ret) ? ret : null;
            }
        }

        public IEnumerable<IKeywordTrackable> GetTrackers()
        {
            lock (_bundlesLocker)
            {
                return _bundles.Values.ToArray();
            }
        }

        public UserReceiveManager()
        {
            System.Diagnostics.Debug.WriteLine("UserReceiveManager initialized.");
            Setting.Accounts.Collection.ListenCollectionChanged()
                   .Subscribe(_ => Task.Run(() => this.NotifySettingChanged()));
            App.UserInterfaceReady += NotifySettingChanged;
        }

        // ReSharper disable AccessToModifiedClosure
        private void NotifySettingChanged()
        {
            var accounts = Setting.Accounts.Collection.ToDictionary(a => a.Id);
            var danglings = new List<string>();
            var rearranged = false;
            lock (_bundlesLocker)
            {
                // remove deauthroized accounts
                _bundles.Values
                    .Where(s => !accounts.ContainsKey(s.UserId))
                    .ToArray()
                    .Do(b => _bundles.Remove(b.UserId))
                    .Do(b => danglings.AddRange(b.TrackKeywords))
                    .ForEach(c => c.Dispose());

                // add new users
                accounts.Where(s => !_bundles.ContainsKey(s.Key))
                        .Select(s => new UserReceiveBundle(s.Value))
                        .Do(s => s.StateChanged += this.OnConnectionStateChanged)
                        .ForEach(b => _bundles.Add(b.UserId, b));

                // stop cancelled streamings
                _bundles.Values
                        .Where(b => b.IsUserStreamsEnabled && !accounts[b.UserId].IsUserStreamsEnabled)
                        .Do(b => danglings.AddRange(b.TrackKeywords))
                        .Do(b => b.IsUserStreamsEnabled = false)
                        .ForEach(b => b.TrackKeywords = null);

                if (danglings.Count > 0)
                {
                    rearranged = true;
                }

                // start new streamings
                _bundles.Values
                    .Where(b => !b.IsUserStreamsEnabled && accounts[b.UserId].IsUserStreamsEnabled)
                    .ForEach(c =>
                    {
                        c.TrackKeywords = danglings.Take(UserStreamsReceiver.MaxTrackingKeywordCounts);
                        c.IsUserStreamsEnabled = true;
                        danglings = danglings.Skip(UserStreamsReceiver.MaxTrackingKeywordCounts).ToList();
                    });

                while (danglings.Count > 0)
                {
                    var bundle = _bundles.Values
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
            var handler = this.TrackRearranged;
            if (handler != null) handler();
        }
        // ReSharper restore AccessToModifiedClosure

        public void ReconnectStream(long id)
        {
            lock (_bundlesLocker)
            {
                _bundles.Values.Where(b => b.UserId == id).ForEach(c => c.ReconnectUserStreams());
            }
        }

        public void ReconnectAllStreams()
        {
            lock (_bundlesLocker)
            {
                _bundles.Values.ForEach(c => c.ReconnectUserStreams());
            }
        }

        private sealed class UserReceiveBundle : IDisposable, IKeywordTrackable
        {
            private readonly TwitterAccount _authInfo;
            private readonly CompositeDisposable _disposable;
            private readonly UserStreamsReceiver _userStreamsReceiver;

            public event Action<long> StateChanged;

            private void OnStateChanged(long obj)
            {
                System.Diagnostics.Debug.WriteLine("bundle state changed.");
                var handler = this.StateChanged;
                if (handler != null) handler(obj);
            }

            public IEnumerable<string> TrackKeywords
            {
                get { return _userStreamsReceiver.TrackKeywords; }
                set { _userStreamsReceiver.TrackKeywords = value; }
            }

            public long UserId { get { return _authInfo.Id; } }

            public UserReceiveBundle(TwitterAccount authInfo)
            {
                _authInfo = authInfo;
                _disposable = new CompositeDisposable
                {
                    (this._userStreamsReceiver = new UserStreamsReceiver(authInfo)),
                    new HomeTimelineReceiver(authInfo),
                    new MentionTimelineReceiver(authInfo),
                    new DirectMessagesReceiver(authInfo),
                    new UserInfoReceiver(authInfo),
                    new UserTimelineReceiver(authInfo),
                    new UserRelationReceiver(authInfo)
                };
                _userStreamsReceiver.StateChanged += () => this.OnStateChanged(this.UserId);
            }

            public bool IsUserStreamsEnabled
            {
                get { return _userStreamsReceiver.IsEnabled; }
                set { _userStreamsReceiver.IsEnabled = value; }
            }

            public UserStreamsConnectionState ConnectionState
            {
                get
                {
                    return _userStreamsReceiver == null
                               ? UserStreamsConnectionState.Invalid
                               : _userStreamsReceiver.ConnectionState;
                }
            }

            public void Dispose()
            {
                _disposable.Dispose();
            }

            public void ReconnectUserStreams()
            {
                _userStreamsReceiver.Reconnect();
            }
        }
    }

    internal interface IKeywordTrackable
    {
        IEnumerable<string> TrackKeywords { get; set; }

        long UserId { get; }
    }
}
