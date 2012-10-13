using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Models.Store;
using StarryEyes.Models.Tab;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;
using StarryEyes.Models.Operations;
using System.Windows.Media.Imaging;
using System.Reactive.Linq;

namespace StarryEyes.Models
{
    /// <summary>
    /// ツイート入力関連のモデル
    /// </summary>
    public static class InputAreaModel
    {
        private static TabModel currentFocusTabModel = null;

        private static readonly ObservableSynchronizedCollectionEx<AuthenticateInfo> _bindingAuthInfos =
            new ObservableSynchronizedCollectionEx<AuthenticateInfo>();
        public static ObservableSynchronizedCollectionEx<AuthenticateInfo> BindingAuthInfos
        {
            get { return InputAreaModel._bindingAuthInfos; }
        } 

        private static readonly ObservableSynchronizedCollectionEx<string> _bindingHashtags =
            new ObservableSynchronizedCollectionEx<string>();
        public static ObservableSynchronizedCollectionEx<string> BindingHashtags
        {
            get { return InputAreaModel._bindingHashtags; }
        }

        private static readonly ObservableSynchronizedCollection<FailedUpdateInfo> _faileds =
            new ObservableSynchronizedCollection<FailedUpdateInfo>();
        public static ObservableSynchronizedCollection<FailedUpdateInfo> FailedUpdates
        {
            get { return InputAreaModel._faileds; }
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

        public static void SendStatus(IEnumerable<AuthenticateInfo> infos, string status,
            TwitterStatus inReplyTo, BitmapImage bitmap, GeoLocationInfo geo)
        {
            infos
                .Select(info => new TweetOperation(info, status, inReplyTo, geo, bitmap))
                .ForEach(op => op.Run()
                    .Subscribe(_ => StatusStore.Store(_),
                        ex => FailedUpdates.Add(new FailedUpdateInfo(op, ex))));
        }

        public static void SendMessage(IEnumerable<AuthenticateInfo> infos, string message,
            TwitterUser recipient)
        {
            infos
                .Select(info => new DirectMessageOperation(info, recipient, message))
                .ForEach(op => op.Run()
                    .Subscribe(_ => StatusStore.Store(_),
                    ex => FailedUpdates.Add(new FailedUpdateInfo(op, ex))));
        }
    }

    public class FailedUpdateInfo
    {
        public FailedUpdateInfo(TweetOperation operation, Exception thrown)
        {
        }

        public FailedUpdateInfo(DirectMessageOperation operation, Exception thrown)
        {
        }
    }

    public enum CursorPosition
    {
        Begin,
        End,
    }
}
