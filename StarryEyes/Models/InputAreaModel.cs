using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Livet;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Hubs;
using StarryEyes.Models.Operations;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Tab;

namespace StarryEyes.Models
{
    /// <summary>
    /// ツイート入力関連のモデル
    /// </summary>
    public static class InputAreaModel
    {
        private static TabModel _currentFocusTabModel;

        private static readonly ObservableSynchronizedCollection<AuthenticateInfo> _bindingAuthInfos =
            new ObservableSynchronizedCollection<AuthenticateInfo>();

        private static readonly ObservableSynchronizedCollection<string> _bindingHashtags =
            new ObservableSynchronizedCollection<string>();

        private static readonly ObservableSynchronizedCollection<TweetInputInfo> _drafts =
            new ObservableSynchronizedCollection<TweetInputInfo>();

        static InputAreaModel()
        {
            _bindingAuthInfos.CollectionChanged += (_, __) =>
                {
                    if (_currentFocusTabModel != null)
                    {
                        switch (__.Action)
                        {
                            case NotifyCollectionChangedAction.Add:
                                __.NewItems
                                  .OfType<AuthenticateInfo>()
                                  .Select(i => i.Id)
                                  .ForEach(_currentFocusTabModel.BindingAccountIds.Add);
                                break;
                            case NotifyCollectionChangedAction.Remove:
                                __.OldItems
                                  .OfType<AuthenticateInfo>()
                                  .ForEach(i => _currentFocusTabModel.BindingAccountIds.Remove(i.Id));
                                break;
                            case NotifyCollectionChangedAction.Replace:
                                __.OldItems
                                  .OfType<AuthenticateInfo>()
                                  .ForEach(i => _currentFocusTabModel.BindingAccountIds.Remove(i.Id));
                                __.NewItems
                                  .OfType<AuthenticateInfo>()
                                  .Select(i => i.Id)
                                  .ForEach(_currentFocusTabModel.BindingAccountIds.Add);
                                break;
                            case NotifyCollectionChangedAction.Reset:
                                _currentFocusTabModel.BindingAccountIds.Clear();
                                _bindingAuthInfos
                                    .Select(i => i.Id)
                                    .ForEach(_currentFocusTabModel.BindingAccountIds.Add);
                                break;
                        }
                    }
                };
            _bindingHashtags.CollectionChanged += (_, __) =>
                {
                    if (_currentFocusTabModel != null)
                        _currentFocusTabModel.BindingHashtags = _bindingHashtags.ToList();
                };
        }

        public static ObservableSynchronizedCollection<AuthenticateInfo> BindingAuthInfos
        {
            get { return _bindingAuthInfos; }
        }

        public static ObservableSynchronizedCollection<string> BindingHashtags
        {
            get { return _bindingHashtags; }
        }

        public static ObservableSynchronizedCollection<TweetInputInfo> Drafts
        {
            get { return _drafts; }
        }

        public static TweetInputInfo PreviousPosted { get; set; }

        public static void NotifyChangeFocusingTab(TabModel tabModel)
        {
            _bindingAuthInfos.Clear();
            _currentFocusTabModel = null;
            tabModel.BindingAccountIds
                    .Select(AccountsStore.GetAccountSetting)
                    .Where(_ => _ != null)
                    .Select(_ => _.AuthenticateInfo)
                    .ForEach(_bindingAuthInfos.Add);
            _bindingHashtags.Clear();
            tabModel.BindingHashtags.ForEach(_bindingHashtags.Add);
            _currentFocusTabModel = tabModel;
        }

        public static event Action<IEnumerable<AuthenticateInfo>, string, CursorPosition, TwitterStatus>
            OnSetTextRequested;

        public static void SetText(IEnumerable<AuthenticateInfo> infos = null, string body = null,
                                   CursorPosition cursor = CursorPosition.End, TwitterStatus inReplyTo = null,
                                   bool focusToInputArea = true)
        {
            var handler = OnSetTextRequested;
            if (handler != null)
                handler(infos, body, cursor, inReplyTo);
            if (focusToInputArea)
                MainWindowModel.SetFocusTo(FocusRequest.Tweet);
        }

        public static event Action<IEnumerable<AuthenticateInfo>, TwitterUser> OnSendDirectMessageRequested;

        public static void SetDirectMessage(IEnumerable<AuthenticateInfo> info, TwitterUser recipient,
                                            bool focusToInputArea = true)
        {
            Action<IEnumerable<AuthenticateInfo>, TwitterUser> handler = OnSendDirectMessageRequested;
            if (handler != null)
                handler(info, recipient);
            if (focusToInputArea)
                MainWindowModel.SetFocusTo(FocusRequest.Tweet);
        }

    }

    /// <summary>
    ///     Describes &quot;a input&quot;.
    /// </summary>
    public class TweetInputInfo
    {
        private BitmapImage _attachedImage;
        private AuthenticateInfo[] _authInfos;

        private string[] _hashtags;
        private Tuple<AuthenticateInfo, TwitterStatus>[] _postedTweets;
        private readonly string _initialText;
        private string _text;

        public TweetInputInfo(string initialText)
        {
            _initialText = initialText;
            _text = initialText;
        }

        /// <summary>
        ///     Binding authenticate informations.
        /// </summary>
        public IEnumerable<AuthenticateInfo> AuthInfos
        {
            get { return _authInfos ?? Enumerable.Empty<AuthenticateInfo>(); }
            set { _authInfos = value.ToArray(); }
        }

        /// <summary>
        ///     Binding hashtags.
        /// </summary>
        public IEnumerable<string> Hashtags
        {
            get { return _hashtags ?? Enumerable.Empty<string>(); }
            set { _hashtags = value.ToArray(); }
        }

        /// <summary>
        ///     In reply to someone.
        /// </summary>
        public TwitterStatus InReplyTo { get; set; }

        /// <summary>
        ///     Message recipient target.
        /// </summary>
        public TwitterUser MessageRecipient { get; set; }

        public string Text
        {
            get { return _text ?? String.Empty; }
            set { _text = value; }
        }

        /// <summary>
        ///     Posted status ids.
        /// </summary>
        public IEnumerable<Tuple<AuthenticateInfo, TwitterStatus>> PostedTweets
        {
            get { return _postedTweets; }
            set { _postedTweets = value.ToArray(); }
        }

        /// <summary>
        ///     Thrown Exception in previous trial.
        /// </summary>
        public Exception ThrownException { get; set; }

        /// <summary>
        ///     Get exception readable info, if available.
        /// </summary>
        public string ThrownExceptionMessage
        {
            get
            {
                var ex = ThrownException as TweetFailedException;
                if (ex != null)
                {
                    return ex.Message;
                }
                return null;
            }
        }

        /// <summary>
        ///     Attached geo-location info.
        /// </summary>
        public GeoLocationInfo AttachedGeoInfo { get; set; }

        /// <summary>
        ///     Attached image.
        ///     <para />
        ///     This bitmap image is frozen.
        /// </summary>
        public BitmapImage AttachedImage
        {
            get { return _attachedImage; }
            set
            {
                // freeze object for accessibility with non-dispatcher threads.
                if (value != null && !value.IsFrozen)
                    value.Freeze();
                _attachedImage = value;
            }
        }

        public string InitialText
        {
            get { return _initialText; }
        }

        public TweetInputInfo Clone()
        {
            return new TweetInputInfo(_initialText)
                {
                    _authInfos = _authInfos,
                    _hashtags = _hashtags,
                    InReplyTo = InReplyTo,
                    MessageRecipient = MessageRecipient,
                    Text = Text,
                    _postedTweets = _postedTweets,
                    ThrownException = ThrownException,
                    AttachedGeoInfo = AttachedGeoInfo,
                    _attachedImage = _attachedImage
                };
        }

        public IObservable<TweetInputInfo> Send()
        {
            var postResults = new List<PostResult>();
            var subject = new Subject<TweetInputInfo>();
            _authInfos.ToObservable()
                      .SelectMany(authInfo =>
                                  (MessageRecipient != null
                                       ? (OperationBase<TwitterStatus>)
                                         new DirectMessageOperation(authInfo, MessageRecipient, Text)
                                       : new TweetOperation(authInfo, Text, InReplyTo, AttachedGeoInfo, AttachedImage)
                                  ).Run()
                                   .SelectMany(StoreHub.MergeStore)
                                   .Select(_ => new PostResult(authInfo, _))
                                   .Catch((Exception ex) => Observable.Return(new PostResult(authInfo, ex))))
                      .Subscribe(postResults.Add,
                                 () =>
                                 {
                                     postResults.GroupBy(_ => _.IsSucceeded)
                                                .ForEach(_ =>
                                                {
                                                    if (_.Key)
                                                    {
                                                        TweetInputInfo ret = Clone();
                                                        ret.AuthInfos = _.Select(pr => pr.AuthInfo).ToArray();
                                                        ret.PostedTweets =
                                                            _.Select(pr => Tuple.Create(pr.AuthInfo, pr.Status))
                                                             .ToArray();
                                                        subject.OnNext(ret);
                                                    }
                                                    else
                                                    {
                                                        _.ForEach(pr =>
                                                        {
                                                            TweetInputInfo ret = Clone();
                                                            ret.AuthInfos = new[] { pr.AuthInfo };
                                                            ret.ThrownException = pr.ThrownException;
                                                            subject.OnNext(ret);
                                                        });
                                                    }
                                                });
                                     subject.OnCompleted();
                                 });
            return subject;
        }

        internal async Task DeletePrevious()
        {
            if (PostedTweets != null)
            {
                Debug.WriteLine("deleting previous...");
                await PostedTweets.ToObservable()
                                  .Select(_ => new DeleteOperation(_.Item1, _.Item2))
                                  .LastOrDefaultAsync();
            }
        }

        private class PostResult
        {
            public PostResult(AuthenticateInfo info, TwitterStatus status)
            {
                AuthInfo = info;
                IsSucceeded = true;
                Status = status;
            }

            public PostResult(AuthenticateInfo info, Exception ex)
            {
                AuthInfo = info;
                IsSucceeded = false;
                ThrownException = ex;
            }

            public bool IsSucceeded { get; private set; }

            public AuthenticateInfo AuthInfo { get; private set; }

            public TwitterStatus Status { get; private set; }

            public Exception ThrownException { get; private set; }
        }
    }

    public enum CursorPosition
    {
        Begin,
        End,
    }
}