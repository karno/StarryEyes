using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using Livet;
using Livet.Messaging.Windows;
using StarryEyes.Models.Subsystems.Notifications.UI;
using StarryEyes.Settings;
using StarryEyes.Views;
using StarryEyes.Views.Notifications;

namespace StarryEyes.ViewModels.Notifications
{
    public class SlimNotificatorViewModel : ViewModel
    {
        private static readonly object _dequeueLocker = new object();

        private static bool _isDisplaying;

        public static void Initialize()
        {
            SlimNotificator.Instance.OnNewNotificationDataQueued += () =>
            {
                lock (_dequeueLocker)
                {
                    if (!_isDisplaying)
                    {
                        _isDisplaying = true;
                        ShowNext();
                    }
                }
            };
        }

        private static void ShowNext()
        {
            NotificationData next;
            lock (_dequeueLocker)
            {
                next = SlimNotificator.Instance.GetQueuedNotification();
                if (next == null)
                {
                    _isDisplaying = false;
                    return;
                }
            }
            Show(next);
        }

        private static void Show(NotificationData next)
        {
            DispatcherHelper.UIDispatcher.InvokeAsync(() =>
            {
                var mwnd = Application.Current.MainWindow;
                if (mwnd != null && (Setting.NotifyWhenWindowIsActive.Value || !mwnd.IsActive))
                {
                    new SlimNotificatorView
                    {
                        DataContext = new SlimNotificatorViewModel(next)
                    }.Show();
                }
                else
                {
                    // do not show popup
                    ShowNext();
                }
            });
        }

        private readonly NotificationData _data;
        private readonly int _left;
        private readonly int _top;
        private readonly int _width;

        private SlimNotificatorViewModel(NotificationData data)
        {
            _data = data;
            var screen = NotificationUtil.GetNotifyTargetScreen();
            if (screen == null) return;
            var bound = screen.WorkingArea;
            if (bound == Rect.Empty) return; // empty data
            bound.Width *= 96.0 / screen.DpiX;
            bound.Height *= 96.0 / screen.DpiY;
            _width = (int)(bound.Width * 0.7);
            _left = (int)(bound.Left + (bound.Width - _width) / 2.0);
            _top = (int)(bound.Bottom - 24);
        }

        public int Left => _left;

        public int Top => _top;

        public int Width => _width;

        public Color BackgroundColor
        {
            get
            {
                switch (_data.Kind)
                {
                    case SlimNotificationKind.New:
                        return MetroColors.Cyan;
                    case SlimNotificationKind.Mention:
                        return MetroColors.Orange;
                    case SlimNotificationKind.Message:
                        return MetroColors.Magenta;
                    case SlimNotificationKind.Follow:
                        return MetroColors.Cobalt;
                    case SlimNotificationKind.Favorite:
                        return MetroColors.Amber;
                    case SlimNotificationKind.Retweet:
                        return MetroColors.Emerald;
                    case SlimNotificationKind.Quote:
                        return MetroColors.Olive;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public Brush BackgroundBrush => new SolidColorBrush(BackgroundColor);

        public SlimNotificationKind NotificationKind => _data.Kind;

        public Uri UserImage => _data.SourceUser != null
            ? _data.SourceUser.ProfileImageUri
            : _data.TargetStatus.User.ProfileImageUri;

        public string Description
        {
            get
            {
                switch (_data.Kind)
                {
                    case SlimNotificationKind.New:
                    case SlimNotificationKind.Mention:
                    case SlimNotificationKind.Message:
                        return RemoveLines(_data.TargetStatus.ToString());
                    case SlimNotificationKind.Follow:
                        return RemoveLines("@" + _data.SourceUser.ScreenName + " follows " +
                                           "@" + _data.TargetUser.ScreenName);
                    case SlimNotificationKind.Favorite:
                        if (_data.TargetUser != null)
                        {
                            return RemoveLines("@" + _data.SourceUser.ScreenName + " favorites " +
                                               "@" + _data.TargetUser.ScreenName + " retweeted " + _data.TargetStatus);
                        }
                        else
                        {
                            return RemoveLines("@" + _data.SourceUser.ScreenName + " favorites " + _data.TargetStatus);
                        }
                    case SlimNotificationKind.Retweet:
                        if (_data.TargetUser != null)
                        {
                            return RemoveLines("@" + _data.SourceUser.ScreenName + " retweets " +
                                               "@" + _data.TargetUser.ScreenName + " retweeted " + _data.TargetStatus);
                        }
                        else
                        {
                            return RemoveLines("@" + _data.SourceUser.ScreenName + " retweets " + _data.TargetStatus);
                        }
                    case SlimNotificationKind.Quote:
                        return RemoveLines("@" + _data.SourceUser.ScreenName + " quotes " + _data.TargetStatus);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private string RemoveLines(string text)
        {
            return text.Replace("\r", "").Replace("\n", "");
        }

        public void Shown()
        {
            Observable.Timer(TimeSpan.FromSeconds(3.1))
                      .Subscribe(_ => Messenger.RaiseSafe(() => new WindowActionMessage(WindowAction.Close)));
            Observable.Timer(TimeSpan.FromSeconds(0.1))
                      .Subscribe(_ => ShowNext());
        }
    }
}