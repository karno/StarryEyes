using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Livet;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Operations;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;

namespace StarryEyes.Models
{
    /// <summary>
    /// ツイート入力関連のモデル
    /// </summary>
    public static class InputAreaModel
    {
        private static TabModel currentFocusTabModel = null;

        private static readonly ObservableSynchronizedCollection<AuthenticateInfo> _bindingAuthInfos =
            new ObservableSynchronizedCollection<AuthenticateInfo>();
        public static ObservableSynchronizedCollection<AuthenticateInfo> BindingAuthInfos
        {
            get { return InputAreaModel._bindingAuthInfos; }
        }

        private static readonly ObservableSynchronizedCollection<string> _bindingHashtags =
            new ObservableSynchronizedCollection<string>();
        public static ObservableSynchronizedCollection<string> BindingHashtags
        {
            get { return InputAreaModel._bindingHashtags; }
        }

        private static readonly ObservableSynchronizedCollection<TweetInputInfo> _drafts =
            new ObservableSynchronizedCollection<TweetInputInfo>();
        public static ObservableSynchronizedCollection<TweetInputInfo> Drafts
        {
            get { return InputAreaModel._drafts; }
        }

        private static TweetInputInfo _previousPosted = null;
        public static TweetInputInfo PreviousPosted
        {
            get { return InputAreaModel._previousPosted; }
            set { InputAreaModel._previousPosted = value; }
        }

        static InputAreaModel()
        {
            _bindingAuthInfos.CollectionChanged += (_, __) =>
            {
                if (currentFocusTabModel != null)
                {
                    switch (__.Action)
                    {
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                            __.NewItems
                                .OfType<AuthenticateInfo>()
                                .Select(i => i.Id)
                                .ForEach(currentFocusTabModel.BindingAccountIds.Add);
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                            __.OldItems
                                .OfType<AuthenticateInfo>()
                                .ForEach(i => currentFocusTabModel.BindingAccountIds.Remove(i.Id));
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                            __.OldItems
                                .OfType<AuthenticateInfo>()
                                .ForEach(i => currentFocusTabModel.BindingAccountIds.Remove(i.Id));
                            __.NewItems
                                .OfType<AuthenticateInfo>()
                                .Select(i => i.Id)
                                .ForEach(currentFocusTabModel.BindingAccountIds.Add);
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                            currentFocusTabModel.BindingAccountIds.Clear();
                            _bindingAuthInfos
                                .Select(i => i.Id)
                                .ForEach(currentFocusTabModel.BindingAccountIds.Add);
                            break;
                    }
                }
            };
            _bindingHashtags.CollectionChanged += (_, __) =>
            {
                if (currentFocusTabModel != null)
                    currentFocusTabModel.BindingHashtags = _bindingHashtags.ToList();
            };
        }

        public static void NotifyChangeFocusingTab(TabModel tabModel)
        {
            _bindingAuthInfos.Clear();
            currentFocusTabModel = null;
            tabModel.BindingAccountIds
                .Select(_ => AccountsStore.GetAccountSetting(_))
                .Where(_ => _ != null)
                .Select(_ => _.AuthenticateInfo)
                .ForEach(_bindingAuthInfos.Add);
            _bindingHashtags.Clear();
            tabModel.BindingHashtags.ForEach(_bindingHashtags.Add);
            currentFocusTabModel = tabModel;
        }

        public static event Action<IEnumerable<AuthenticateInfo>, string, CursorPosition, TwitterStatus> OnSetTextRequested;
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
            var handler = OnSendDirectMessageRequested;
            if (handler != null)
                handler(info, recipient);
            if (focusToInputArea)
                MainWindowModel.SetFocusTo(FocusRequest.Tweet);
        }

        #region ALPS controls


        #endregion
    }

    public class TweetInputInfo
    {
        private AuthenticateInfo[] _authInfos = null;
        /// <summary>
        /// Binding authenticate informations.
        /// </summary>
        public IEnumerable<AuthenticateInfo> AuthInfos
        {
            get { return _authInfos ?? Enumerable.Empty<AuthenticateInfo>(); }
            set { _authInfos = value.ToArray(); }
        }

        private string[] _hashtags = null;
        /// <summary>
        /// Binding hashtags.
        /// </summary>
        public IEnumerable<string> Hashtags
        {
            get { return _hashtags ?? Enumerable.Empty<string>(); }
            set { _hashtags = value.ToArray(); }
        }

        public TwitterStatus InReplyTo { get; set; }

        public TwitterUser MessageRecipient { get; set; }

        private string _text;
        public string Text
        {
            get { return _text ?? String.Empty; }
            set { _text = value; }
        }

        private Tuple<AuthenticateInfo, TwitterStatus>[] _postedTweets = null;

        /// <summary>
        /// Posted status ids.
        /// </summary>
        public IEnumerable<Tuple<AuthenticateInfo, TwitterStatus>> PostedTweets
        {
            get { return _postedTweets; }
            set { _postedTweets = value.ToArray(); }
        }

        /// <summary>
        /// Thrown Exception in previous trial.
        /// </summary>
        public Exception ThrownException { get; set; }

        /// <summary>
        /// Get exception readable info, if available.
        /// </summary>
        public string ThrownExceptionMessage
        {
            get
            {
                var ex = this.ThrownException as TweetFailedException;
                if (ex != null)
                {
                    return ex.Message;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Attached geo-location info.
        /// </summary>
        public GeoLocationInfo AttachedGeoInfo { get; set; }

        private BitmapImage _attachedImage = null;
        /// <summary>
        /// Attached image.<para />
        /// This bitmap image is frozen.
        /// </summary>
        public BitmapImage AttachedImage
        {
            get { return _attachedImage; }
            set
            {
                // freeze object for accessibility with non-dispatcher threads.
                if (!value.IsFrozen)
                    value.Freeze();
                _attachedImage = value;
            }
        }

        public TweetInputInfo Clone()
        {
            return new TweetInputInfo()
            {
                _authInfos = this._authInfos,
                _hashtags = this._hashtags,
                InReplyTo = this.InReplyTo,
                MessageRecipient = this.MessageRecipient,
                Text = this.Text,
                _postedTweets = this._postedTweets,
                ThrownException = this.ThrownException,
                AttachedGeoInfo = this.AttachedGeoInfo,
                _attachedImage = this._attachedImage
            };
        }

        public IObservable<TweetInputInfo> Send()
        {
            List<PostResult> postResults = new List<PostResult>();
            var subject = new Subject<TweetInputInfo>();
            _authInfos.ToObservable()
               .SelectMany(AuthInfo =>
                   (MessageRecipient != null ? (OperationBase<TwitterStatus>)
                       new DirectMessageOperation(AuthInfo, MessageRecipient, Text) :
                       new TweetOperation(AuthInfo, Text, InReplyTo, AttachedGeoInfo, AttachedImage)
                   ).Run()
                   .Do(_ => StatusStore.Store(_))
                   .Select(_ => new PostResult(AuthInfo, _))
                   .Catch((Exception ex) => Observable.Return(new PostResult(AuthInfo, ex))))
               .Subscribe(postResults.Add,
               () =>
               {
                   postResults.GroupBy(_ => _.IsSucceeded)
                       .ForEach(_ =>
                       {
                           if (_.Key)
                           {
                               var ret = this.Clone();
                               ret.AuthInfos = _.Select(pr => pr.AuthInfo).ToArray();
                               ret.PostedTweets = _.Select(pr => Tuple.Create(pr.AuthInfo, pr.Status)).ToArray();
                               subject.OnNext(ret);
                           }
                           else
                           {
                               _.ForEach(pr =>
                                   {
                                       var ret = this.Clone();
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
                System.Diagnostics.Debug.WriteLine("deleting previous...");
                await PostedTweets.ToObservable()
                    .Select(_ => new DeleteOperation(_.Item1, _.Item2))
                    .LastOrDefaultAsync();
            }
        }

        private class PostResult
        {
            public PostResult(AuthenticateInfo info, TwitterStatus status)
            {
                this.AuthInfo = info;
                this.IsSucceeded = true;
                this.Status = status;
            }

            public PostResult(AuthenticateInfo info, Exception ex)
            {
                this.AuthInfo = info;
                this.IsSucceeded = false;
                this.ThrownException = ex;
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
