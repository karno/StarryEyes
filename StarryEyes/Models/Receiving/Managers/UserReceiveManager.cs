using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Cadena;
using Cadena.Api.Parameters;
using Cadena.Data.Streams;
using Cadena.Data.Streams.Events;
using Cadena.Engine;
using Cadena.Engine.CyclicReceivers.Relations;
using Cadena.Engine.CyclicReceivers.Timelines;
using Cadena.Engine.StreamReceivers;
using JetBrains.Annotations;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Subsystems;
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
            return !_bundles.TryGetValue(id, out bundle)
                ? UserStreamsConnectionState.Invalid
                : bundle.ConnectionState;
        }

        public IKeywordTrackable GetSuitableKeywordTracker()
        {
            lock (_bundlesLocker)
            {
                return _bundles.Values
                               .Where(c => c.IsUserStreamsEnabled)
                               .Where(c => c.TrackKeywords.Count() < StreamTrackReceiveManager.MaxTrackKeywordLength)
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
            Setting.Accounts.Collection.ListenCollectionChanged(_ => Task.Run(() => NotifySettingChanged()));
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
                        .Do(s => s.StateChanged += arg => ConnectionStateChanged?.Invoke(arg))
                        .ForEach(b => _bundles.Add(b.UserId, b));

                // stop cancelled streamings
                _bundles.Values
                        .Where(b => b.IsUserStreamsEnabled && !accounts[b.UserId].IsUserStreamsEnabled)
                        .Do(b => danglings.AddRange(b.TrackKeywords))
                        .Do(b => b.IsUserStreamsEnabled = false)
                        .ForEach(b => b.TrackKeywords = new string[0]);

                if (danglings.Count > 0)
                {
                    rearranged = true;
                }

                // start new streamings
                _bundles.Values
                        .Where(b => !b.IsUserStreamsEnabled && accounts[b.UserId].IsUserStreamsEnabled)
                        .ForEach(c =>
                        {
                            c.TrackKeywords = danglings.Take(StreamTrackReceiveManager.MaxTrackKeywordLength);
                            c.IsUserStreamsEnabled = true;
                            danglings = danglings.Skip(StreamTrackReceiveManager.MaxTrackKeywordLength).ToList();
                        });

                while (danglings.Count > 0)
                {
                    var bundle = _bundles.Values
                                         .Where(b => b.IsUserStreamsEnabled)
                                         .OrderBy(b => b.TrackKeywords.Count())
                                         .FirstOrDefault();
                    if (bundle == null) break;
                    var keywordCount = bundle.TrackKeywords.Count();
                    if (keywordCount >= StreamTrackReceiveManager.MaxTrackKeywordLength) break;
                    var assignable = StreamTrackReceiveManager.MaxTrackKeywordLength - keywordCount;
                    bundle.TrackKeywords = bundle.TrackKeywords.Concat(danglings.Take(assignable)).ToArray();
                    danglings = danglings.Skip(assignable).ToList();
                }
            }
            if (!rearranged) return;
            TrackRearranged?.Invoke();
        }
        // ReSharper restore AccessToModifiedClosure

        public void ReconnectStream(long id)
        {
            UserReceiveBundle bundle;
            lock (_bundlesLocker)
            {
                _bundles.TryGetValue(id, out bundle);
            }
            bundle?.ReconnectUserStreams();
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
            private readonly TwitterAccount _account;
            private readonly IApiAccessor _streamAccessor;
            private readonly IReceiver[] _receivers;
            private readonly CompositeDisposable _disposable;
            private UserStreamReceiver _userStreamsReceiver;
            private bool _userStreamsEnabled;

            public event Action<TwitterAccount> StateChanged;

            public IEnumerable<string> TrackKeywords
            {
                get => _userStreamsReceiver.TrackKeywords;
                set => _userStreamsReceiver.TrackKeywords = value;
            }

            public long UserId => _account.Id;

            public UserReceiveBundle(TwitterAccount account)
            {
                _account = account;
                var accessor = account.CreateAccessor();
                _streamAccessor = _account.CreateAccessor(EndpointType.StreamEndpoint, false);

                // user streams
                // _userStreamsReceiver = new UserStreamReceiver(streamAccessor, handler);
                _receivers = new IReceiver[]
                {
                    // timelines
                    new HomeTimelineReceiver(accessor, StatusInbox.Enqueue, BackstageModel.NotifyException),
                    new MentionsReceiver(accessor, StatusInbox.Enqueue, BackstageModel.NotifyException),
                    new DirectMessagesReceiver(accessor, StatusInbox.Enqueue, BackstageModel.NotifyException),
                    new SentDirectMessagesReceiver(accessor, StatusInbox.Enqueue, BackstageModel.NotifyException),
                    new UserTimelineReceiver(accessor, StatusInbox.Enqueue, BackstageModel.NotifyException,
                        new UserParameter(_account.Id)),
                    // relations
                    new BlocksReceiver(accessor,
                        blocks => Task.Run(() => _account.RelationData.Blockings.SetAsync(blocks)),
                        BackstageModel.NotifyException),
                    new FollowersReceiver(accessor,
                        blocks => Task.Run(() => _account.RelationData.Followers.SetAsync(blocks)),
                        BackstageModel.NotifyException),
                    new FriendsReceiver(accessor,
                        blocks => Task.Run(() => _account.RelationData.Followings.SetAsync(blocks)),
                        BackstageModel.NotifyException),
                    new MutesReceiver(accessor,
                        blocks => Task.Run(() => _account.RelationData.Mutes.SetAsync(blocks)),
                        BackstageModel.NotifyException),
                    new NoRetweetsReceiver(accessor,
                        blocks => Task.Run(() => _account.RelationData.NoRetweets.SetAsync(blocks)),
                        BackstageModel.NotifyException)
                };
                foreach (var receiver in _receivers)
                {
                    ReceiveManager.ReceiveEngine.RegisterReceiver(receiver);
                }
                _disposable = new CompositeDisposable(accessor, _streamAccessor, _userStreamsReceiver);
                IsUserStreamsEnabled = account.IsUserStreamsEnabled;
            }

            public bool IsUserStreamsEnabled
            {
                get => _userStreamsEnabled;
                set
                {
                    if (_userStreamsEnabled == value) return;
                    _userStreamsEnabled = value;

                    if (value)
                    {
                        var handler = CreateHandler();
                        _userStreamsReceiver = new UserStreamReceiver(_streamAccessor, handler)
                        {
                            RepliesAll = _account.ReceiveRepliesAll,
                            IncludeFollowingsActivities = _account.ReceiveFollowingsActivity
                        };
                        ReceiveManager.ReceiveEngine.RegisterReceiver(_userStreamsReceiver);
                    }
                    else
                    {
                        var old = Interlocked.Exchange(ref _userStreamsReceiver, null);
                        ReceiveManager.ReceiveEngine.UnregisterReceiver(old);
                        old.Dispose();
                    }
                }
            }

            private StreamHandler CreateHandler()
            {
                var handler = StreamHandler.Create(StatusInbox.Enqueue, BackstageModel.NotifyException,
                    _ => StateChanged?.Invoke(_account));
                handler.RegisterHandler<StreamDelete>(d => StatusInbox.EnqueueRemoval(d.Id));
                handler.RegisterHandler<StreamStatusEvent>(se =>
                {
                    switch (se.Event)
                    {
                        case StatusEvents.Unknown:
                            BackstageModel.RegisterEvent(new UnknownEvent(se.Source, se.RawEvent));
                            break;
                        case StatusEvents.Favorite:
                            NotificationService.NotifyFavorited(se.Source, se.TargetObject);
                            break;
                        case StatusEvents.FavoriteRetweet:
                            if (se.TargetObject.RetweetedStatus != null)
                            {
                                NotificationService.NotifyRetweetFavorited(se.Source, se.Target,
                                    se.TargetObject);
                            }
                            break;
                        case StatusEvents.Unfavorite:
                            NotificationService.NotifyUnfavorited(se.Source, se.TargetObject);
                            break;
                        case StatusEvents.RetweetRetweet:
                            if (se.TargetObject.RetweetedStatus != null)
                            {
                                NotificationService.NotifyRetweetRetweeted(se.Source, se.Target,
                                    se.TargetObject);
                            }
                            break;
                        case StatusEvents.Quote:
                            if (se.TargetObject.QuotedStatus != null)
                            {
                                NotificationService.NotifyQuoted(se.Source,
                                    se.TargetObject.QuotedStatus, se.TargetObject);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
                handler.RegisterHandler<StreamUserEvent>(ue =>
                {
                    switch (ue.Event)
                    {
                        case UserEvents.Unknown:
                            BackstageModel.RegisterEvent(new UnknownEvent(ue.Source, ue.RawEvent));
                            break;
                        case UserEvents.Follow:
                            NotificationService.NotifyFollowed(ue.Source, ue.Target);
                            break;
                        case UserEvents.Unfollow:
                            NotificationService.NotifyUnfollowed(ue.Source, ue.Target);
                            break;
                        case UserEvents.Block:
                            NotificationService.NotifyBlocked(ue.Source, ue.Target);
                            break;
                        case UserEvents.Unblock:
                            NotificationService.NotifyUnblocked(ue.Source, ue.Target);
                            break;
                        case UserEvents.Mute:
                            NotificationService.NotifyMuted(ue.Source, ue.Target);
                            break;
                        case UserEvents.UnMute:
                            NotificationService.NotifyUnmuted(ue.Source, ue.Target);
                            break;
                        case UserEvents.UserUpdate:
                            NotificationService.NotifyUserUpdated(ue.Source);
                            break;
                        case UserEvents.UserDelete:
                        case UserEvents.UserSuspend:
                            // do nothing
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
                handler.RegisterHandler<StreamLimit>(
                    le => NotificationService.NotifyLimitationInfoGot(_account, (int)le.UndeliveredCount));

                // list handler is not used.
                return handler;
            }

            public UserStreamsConnectionState ConnectionState => _userStreamsReceiver == null
                ? UserStreamsConnectionState.Disconnected
                : (UserStreamsConnectionState)(int)_userStreamsReceiver.CurrentState;

            public void Dispose()
            {
                _disposable.Dispose();
                foreach (var receiver in _receivers)
                {
                    ReceiveManager.ReceiveEngine.UnregisterReceiver(receiver);
                }
            }

            public void ReconnectUserStreams()
            {
                _userStreamsReceiver.Reconnect();
            }
        }
    }

    internal interface IKeywordTrackable
    {
        [NotNull]
        IEnumerable<string> TrackKeywords { get; set; }

        long UserId { get; }
    }
}