using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Livet;
using StarryEyes.Albireo;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Helpers;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Requests;
using StarryEyes.Models.Timelines.Statuses;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Settings;

namespace StarryEyes.Models
{
    /// <summary>
    /// ツイート入力関連のモデル
    /// </summary>
    public static class InputAreaModel
    {
        private static TabModel _currentFocusTabModel;
        private static TweetInputInfo _previousPosted;

        private static readonly ObservableSynchronizedCollection<TwitterAccount> _bindingAccounts =
            new ObservableSynchronizedCollection<TwitterAccount>();

        private static readonly ObservableSynchronizedCollection<string> _bindingHashtags =
            new ObservableSynchronizedCollection<string>();

        private static readonly ObservableSynchronizedCollection<TweetInputInfo> _drafts =
            new ObservableSynchronizedCollection<TweetInputInfo>();

        public static event Action OnPreviousPostUpdated;

        private static void RaisePreviousPostUpdated()
        {
            OnPreviousPostUpdated.SafeInvoke();
        }

        static InputAreaModel()
        {
            _bindingAccounts.CollectionChanged += (_, __) =>
            {
                if (_currentFocusTabModel == null) return;
                switch (__.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        __.NewItems
                          .OfType<TwitterAccount>()
                          .Select(i => i.Id)
                          .ForEach(_currentFocusTabModel.BindingAccounts.Add);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        __.OldItems
                          .OfType<TwitterAccount>()
                          .ForEach(i => _currentFocusTabModel.BindingAccounts.Remove(i.Id));
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        __.OldItems
                          .OfType<TwitterAccount>()
                          .ForEach(i => _currentFocusTabModel.BindingAccounts.Remove(i.Id));
                        __.NewItems
                          .OfType<TwitterAccount>()
                          .Select(i => i.Id)
                          .ForEach(_currentFocusTabModel.BindingAccounts.Add);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        _currentFocusTabModel.BindingAccounts.Clear();
                        _bindingAccounts
                            .Select(i => i.Id)
                            .ForEach(_currentFocusTabModel.BindingAccounts.Add);
                        break;
                }
            };
            _bindingHashtags.CollectionChanged += (_, __) =>
            {
                if (_currentFocusTabModel != null)
                    _currentFocusTabModel.BindingHashtags = _bindingHashtags.ToList();
            };
        }

        public static ObservableSynchronizedCollection<TwitterAccount> BindingAccounts
        {
            get { return _bindingAccounts; }
        }

        public static ObservableSynchronizedCollection<string> BindingHashtags
        {
            get { return _bindingHashtags; }
        }

        public static ObservableSynchronizedCollection<TweetInputInfo> Drafts
        {
            get { return _drafts; }
        }

        public static TweetInputInfo PreviousPosted
        {
            get { return _previousPosted; }
            set
            {
                _previousPosted = value;
                RaisePreviousPostUpdated();
            }
        }

        public static void NotifyChangeFocusingTab(TabModel tabModel)
        {
            if (_currentFocusTabModel == tabModel) return;
            _currentFocusTabModel = null;
            if (!_bindingAccounts.Select(a => a.Id).SequenceEqual(tabModel.BindingAccounts))
            {
                _bindingAccounts.Clear();
                tabModel.BindingAccounts
                        .Select(Setting.Accounts.Get)
                        .Where(_ => _ != null)
                        .ForEach(_bindingAccounts.Add);
            }
            if (!_bindingHashtags.SequenceEqual(tabModel.BindingHashtags))
            {
                _bindingHashtags.Clear();
                tabModel.BindingHashtags.ForEach(_bindingHashtags.Add);
            }
            _currentFocusTabModel = tabModel;
        }

        public static event Action<IEnumerable<TwitterAccount>, string, CursorPosition, TwitterStatus>
            SetTextRequested;

        public static void SetText(IEnumerable<TwitterAccount> infos = null, string body = null,
                                   CursorPosition cursor = null, TwitterStatus inReplyTo = null,
                                   bool focusToInputArea = true)
        {
            if (cursor == null)
            {
                cursor = CursorPosition.End;
            }
            var handler = SetTextRequested;
            if (handler != null)
            {
                handler(infos, body, cursor, inReplyTo);
            }
            if (focusToInputArea)
            {
                MainWindowModel.SetFocusTo(FocusRequest.Input);
            }
        }

        public static void SetText(IEnumerable<long> infos, string body = null,
                                   CursorPosition cursor = null, TwitterStatus inReplyTo = null,
                                   bool focusToInputArea = true)
        {
            if (cursor == null)
            {
                cursor = CursorPosition.End;
            }
            var accounts = infos.Guard()
                                .Select(Setting.Accounts.Get)
                                .Where(s => s != null);
            SetText(accounts, body, cursor, inReplyTo, focusToInputArea);
        }

        public static event Action<IEnumerable<TwitterAccount>, TwitterUser, string> SendDirectMessageRequested;

        public static void SetDirectMessage(IEnumerable<TwitterAccount> info, TwitterUser recipient,
        string body = null, bool focusToInputArea = true)
        {
            var handler = SendDirectMessageRequested;
            if (handler != null)
            {
                handler(info, recipient, body);
            }
            if (focusToInputArea)
            {
                MainWindowModel.SetFocusTo(FocusRequest.Input);
            }
        }
    }

    /// <summary>
    ///     Describes &quot;a input&quot;.
    /// </summary>
    public class TweetInputInfo
    {
        private BitmapImage _attachedImage;
        private TwitterAccount[] _accounts;

        private string[] _hashtags;
        private Tuple<TwitterAccount, TwitterStatus>[] _postedTweets;
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
        [NotNull]
        public IEnumerable<TwitterAccount> Accounts
        {
            get { return this._accounts ?? Enumerable.Empty<TwitterAccount>(); }
            set { this._accounts = value.ToArray(); }
        }

        /// <summary>
        ///     Binding hashtags.
        /// </summary>
        [NotNull]
        public IEnumerable<string> Hashtags
        {
            get { return _hashtags ?? Enumerable.Empty<string>(); }
            set { _hashtags = value.Guard().ToArray(); }
        }

        /// <summary>
        ///     In reply to someone.
        /// </summary>
        public StatusModel InReplyTo { get; set; }

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
        public IEnumerable<Tuple<TwitterAccount, TwitterStatus>> PostedTweets
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
                return ThrownException.Message;
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
                {
                    value.Freeze();
                }
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
                    _accounts = this._accounts,
                    _hashtags = _hashtags == null ? null : _hashtags.ToArray(),
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
            var existedTags = TwitterRegexPatterns.ValidHashtag.Matches(Text)
                                               .OfType<Match>()
                                               .Select(_ => _.Groups[1].Value)
                                               .Distinct()
                                               .ToArray();
            var binds = _hashtags.Guard().Except(existedTags)
                                 .Distinct()
                                 .Select(t => " #" + t)
                                 .JoinString("");
            var postResults = new List<PostResult>();
            var subject = new Subject<TweetInputInfo>();
            var request = MessageRecipient != null
                              ? (RequestBase<TwitterStatus>)new MessagePostingRequest(MessageRecipient, Text)
                              : new TweetPostingRequest(Text + binds,
                                                        InReplyTo != null ? InReplyTo.Status : null,
                                                        AttachedGeoInfo, AttachedImage);
            this._accounts.ToObservable()
                .SelectMany(account => RequestQueue.Enqueue(account, request)
                                                   .Do(StatusInbox.Queue)
                                                   .Select(_ => new PostResult(account, _))
                                                   .Catch(
                                                       (Exception ex) =>
                                                       Observable.Return(new PostResult(account, ex))))
                .Subscribe(postResults.Add,
                           () =>
                           {
                               // ReSharper disable AccessToDisposedClosure
                               postResults.GroupBy(_ => _.IsSucceeded)
                                          .ForEach(_ =>
                                          {
                                              if (_.Key)
                                              {
                                                  var ret = Clone();
                                                  ret.Accounts = _.Select(pr => pr.Account).ToArray();
                                                  ret.PostedTweets =
                                                      _.Select(pr => Tuple.Create(pr.Account, pr.Status))
                                                       .ToArray();
                                                  subject.OnNext(ret);
                                              }
                                              else
                                              {
                                                  _.ForEach(pr =>
                                                  {
                                                      var ret = Clone();
                                                      ret.Accounts = new[] { pr.Account };
                                                      ret.ThrownException = pr.ThrownException;
                                                      subject.OnNext(ret);
                                                  });
                                              }
                                          });
                               // ReSharper restore AccessToDisposedClosure
                               subject.OnCompleted();
                           });
            return subject;
        }

        internal async Task DeletePreviousAsync()
        {
            if (PostedTweets == null) return;
            Debug.WriteLine("deleting previous...");
            await PostedTweets.Select(t => RequestQueue.Enqueue(t.Item1, new DeletionRequest(t.Item2)))
                         .ToObservable()
                         .SelectMany(_ => _)
                         .LastOrDefaultAsync();
        }

        private class PostResult
        {
            public PostResult(TwitterAccount info, TwitterStatus status)
            {
                this.Account = info;
                IsSucceeded = true;
                Status = status;
            }

            public PostResult(TwitterAccount info, Exception ex)
            {
                this.Account = info;
                IsSucceeded = false;
                ThrownException = ex;
            }

            public bool IsSucceeded { get; private set; }

            public TwitterAccount Account { get; private set; }

            public TwitterStatus Status { get; private set; }

            public Exception ThrownException { get; private set; }
        }
    }

    public class CursorPosition
    {
        public static readonly CursorPosition Begin = new CursorPosition(0, 0);

        public static readonly CursorPosition End = new CursorPosition(-1, 0);

        private readonly int _index;
        private readonly int _selectionLength;

        public CursorPosition(int index, int selectionLength)
        {
            this._index = index;
            this._selectionLength = selectionLength;
        }

        public int Index
        {
            get { return this._index; }
        }

        public int SelectionLength
        {
            get { return this._selectionLength; }
        }

        public override int GetHashCode()
        {
            return _index.GetHashCode() ^ this._selectionLength.GetHashCode();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            var cp = obj as CursorPosition;
            return cp != null && (cp.Index == this._index && cp.SelectionLength == this.SelectionLength);
        }
    }
}