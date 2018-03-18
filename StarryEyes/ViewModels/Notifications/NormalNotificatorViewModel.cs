using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using Cadena.Data;
using JetBrains.Annotations;
using Livet;
using Livet.Messaging.Windows;
using StarryEyes.Models.Subsystems.Notifications.UI;
using StarryEyes.Settings;
using StarryEyes.Views;
using StarryEyes.Views.Notifications;

namespace StarryEyes.ViewModels.Notifications
{
    public class NormalNotificatorViewModel : ViewModel
    {
        private static readonly List<bool> Slots = new List<bool>();

        public static void Initialize()
        {
            NormalNotificator.Instance.OnStatusReceived += Instance_OnStatusReceived;
            NormalNotificator.Instance.OnFavorited += Instance_OnFavorited;
            NormalNotificator.Instance.OnMentionReceived += Instance_OnMentionReceived;
            NormalNotificator.Instance.OnMessageReceived += Instance_OnMessageReceived;
            NormalNotificator.Instance.OnRetweeted += Instance_OnRetweeted;
            NormalNotificator.Instance.OnQuoted += Instance_OnQuoted;
            NormalNotificator.Instance.OnUserFollowed += Instance_OnUserFollowed;
        }

        static void Instance_OnUserFollowed(TwitterUser arg1, TwitterUser arg2)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Cobalt,
                arg1, "followed", "@" + arg2.ScreenName + Environment.NewLine + arg2.Name));
        }

        static void Instance_OnFavorited(TwitterUser arg1, TwitterStatus arg2)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Amber,
                arg1, "favorited", arg2.ToString()));
        }

        static void Instance_OnRetweeted(TwitterUser arg1, TwitterStatus arg2)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Emerald,
                arg1, "retweeted", arg2.ToString()));
        }

        static void Instance_OnQuoted(TwitterUser arg1, TwitterStatus arg2)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Olive,
                arg1, "quoted", arg2.ToString()));
        }


        static void Instance_OnStatusReceived(TwitterStatus obj)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Cyan,
                obj.User, "@" + obj.User.ScreenName, obj.GetEntityAidedText()));
        }

        static void Instance_OnMessageReceived(TwitterStatus obj)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Magenta,
                obj.User, "message from @" + obj.User.ScreenName, obj.GetEntityAidedText()));
        }

        static void Instance_OnMentionReceived(TwitterStatus obj)
        {
            Show(new NormalNotificatorViewModel(MetroColors.Orange,
                obj.User, "mention from @" + obj.User.ScreenName, obj.GetEntityAidedText()));
        }

        static void Show(NormalNotificatorViewModel dataContext)
        {
            DispatcherHelper.UIDispatcher.InvokeAsync(() =>
            {
                var mwnd = Application.Current.MainWindow;
                if (mwnd != null && (Setting.NotifyWhenWindowIsActive.Value || !mwnd.IsActive))
                {
                    new NormalNotificatorView
                    {
                        DataContext = dataContext
                    }.Show();
                }
                else
                {
                    dataContext.ReleaseSlot();
                }
            });
        }

        private readonly Color _background;
        private readonly TwitterUser _user;
        private readonly string _header;
        private readonly string _description;
        private readonly int _slotIndex;
        private readonly int _left;
        private readonly int _top;

        public NormalNotificatorViewModel(
            Color background, TwitterUser user, string header, string description)
        {
            _background = background;
            _user = user;
            _header = header;
            _description = description;
            // acquire slot
            lock (Slots)
            {
                _slotIndex = 0;
                while (_slotIndex < Slots.Count)
                {
                    if (!Slots[_slotIndex]) break;
                    _slotIndex++;
                }
                if (_slotIndex < Slots.Count)
                {
                    Slots[_slotIndex] = true;
                }
                else
                {
                    Slots.Add(true);
                }
            }
            var screen = NotificationUtil.GetNotifyTargetScreen();
            if (screen == null) return;
            var bound = screen.WorkingArea;
            // size of notificator
            const int wh = 80;
            const int ww = 300;

            // items per one (vertical) line
            var ipl = (int)Math.Ceiling(bound.Height / wh);

            if (ipl == 0)
            {
                // can not place any dialog
                return;
            }

            bound.Width *= 96.0 / screen.DpiX;
            bound.Height *= 96.0 / screen.DpiY;
            _left = (int)((bound.Width - ww * (_slotIndex / ipl + 1)) + bound.Left);
            _top = (int)((bound.Height - wh * (_slotIndex % ipl + 1)) + bound.Top);
            System.Diagnostics.Debug.WriteLine("#N - " + _slotIndex + " / " + _left + ", " + _top);
        }

        public int Left
        {
            get { return _left; }
        }

        public int Top
        {
            get { return _top; }
        }

        public Color Background
        {
            get { return _background; }
        }

        public Brush BackgroundBrush
        {
            get { return new SolidColorBrush(Background).ToFrozen(); }
        }

        public TwitterUser User
        {
            get { return _user; }
        }

        public Uri UserImage
        {
            get { return User.ProfileImageUri; }
        }

        public string Header
        {
            get { return _header; }
        }

        public string Description
        {
            get { return _description; }
        }

        [UsedImplicitly]
        public void Shown()
        {
            Observable.Timer(TimeSpan.FromSeconds(3))
                      .Subscribe(_ =>
                      {
                          Messenger.RaiseSafe(() => new WindowActionMessage(WindowAction.Close));
                          ReleaseSlot();
                      });
        }

        public void ReleaseSlot()
        {
            lock (Slots)
            {
                Slots[_slotIndex] = false;
            }
            Dispose();
        }
    }
}