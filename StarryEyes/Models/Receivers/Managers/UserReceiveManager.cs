using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using StarryEyes.Annotations;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Receivers.ReceiveElements;
using StarryEyes.Models.Stores;

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
            AccountsStore.Accounts.ListenCollectionChanged()
                         .Subscribe(_ => NotifySettingChanged());
            App.OnUserInterfaceReady += NotifySettingChanged;
        }

        // ReSharper disable AccessToModifiedClosure
        private void NotifySettingChanged()
        {
            var settings = AccountsStore.Accounts.ToDictionary(k => k.UserId);
            var danglings = new List<string>();
            var rearranged = false;
            lock (_bundlesLocker)
            {
                // remove deauthroized accounts
                _bundles.Values
                    .Where(s => !settings.ContainsKey(s.UserId))
                    .ToArray()
                    .Do(b => _bundles.Remove(b.UserId))
                    .Do(b => danglings.AddRange(b.TrackKeywords))
                    .ForEach(c => c.Dispose());

                // add new users
                settings.Where(s => !_bundles.ContainsKey(s.Key))
                        .Select(s => new UserReceiveBundle(s.Value.AuthenticateInfo))
                        .Do(s => s.StateChanged += this.OnConnectionStateChanged)
                        .ForEach(b => _bundles.Add(b.UserId, b));

                // stop cancelled streamings
                _bundles.Values
                        .Where(b => b.IsUserStreamsEnabled && !settings[b.UserId].IsUserStreamsEnabled)
                        .Do(b => danglings.AddRange(b.TrackKeywords))
                        .Do(b => b.IsUserStreamsEnabled = false)
                        .ForEach(b => b.TrackKeywords = null);

                if (danglings.Count > 0)
                {
                    rearranged = true;
                }

                // start new streamings
                _bundles.Values
                    .Where(b => !b.IsUserStreamsEnabled && settings[b.UserId].IsUserStreamsEnabled)
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

        public void ReconnectStreams(long id)
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
            private readonly AuthenticateInfo _authInfo;
            private readonly CompositeDisposable _disposable;
            [UsedImplicitly]
            private readonly UserStreamsReceiver _userStreamsReceiver;
            [UsedImplicitly]
            private readonly HomeTimelineReceiver _homeTimelineReceiver;
            [UsedImplicitly]
            private readonly MentionTimelineReceiver _mentionTimelineReceiver;
            [UsedImplicitly]
            private readonly DirectMessagesReceiver _directMessagesReceiver;
            [UsedImplicitly]
            private readonly UserInfoReceiver _userInfoReceiver;
            [UsedImplicitly]
            private readonly UserRelationReceiver _userRelationReceiver;

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

            public UserReceiveBundle(AuthenticateInfo authInfo)
            {
                _authInfo = authInfo;
                // ReSharper disable UseObjectOrCollectionInitializer
                _disposable = new CompositeDisposable();
                _disposable.Add(_userStreamsReceiver = new UserStreamsReceiver(authInfo));
                _disposable.Add(_homeTimelineReceiver = new HomeTimelineReceiver(authInfo));
                _disposable.Add(_mentionTimelineReceiver = new MentionTimelineReceiver(authInfo));
                _disposable.Add(_directMessagesReceiver = new DirectMessagesReceiver(authInfo));
                _disposable.Add(_userInfoReceiver = new UserInfoReceiver(authInfo));
                _disposable.Add(_userRelationReceiver = new UserRelationReceiver(authInfo));
                // ReSharper restore UseObjectOrCollectionInitializer
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
                    return _userRelationReceiver == null
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
