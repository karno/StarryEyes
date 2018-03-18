using System;
using System.Reactive.Linq;
using Livet;
using Livet.Messaging.Windows;
using StarryEyes.ViewModels.Notifications;
using StarryEyes.Views.Dialogs;

namespace StarryEyes.ViewModels.Dialogs
{
    public class DisplayMarkerViewModel : ViewModel
    {
        public static void ShowMarker(int displayIndex)
        {
            DispatcherHelper.UIDispatcher.InvokeAsync(
                () => new DisplayMarkerWindow
                {
                    DataContext = new DisplayMarkerViewModel(displayIndex)
                }.Show()
                );
        }

        private readonly int _left;
        private readonly int _top;

        public int Left => _left;

        public int Top => _top;

        private DisplayMarkerViewModel(int displayIndex)
        {
            var screen = NotificationUtil.GetScreenOfIndex(displayIndex);
            if (screen == null) return;
            var warea = screen.WorkingArea;
            if (warea.Width < 1 || warea.Height < 1) return;
            warea.Width *= 96.0 / screen.DpiX;
            warea.Height *= 96.0 / screen.DpiY;
            _left = (int)((warea.Width - 300) / 2 + warea.Left);
            _top = (int)((warea.Height - 60) / 2 + warea.Top);
        }

        public void Shown()
        {
            Observable.Timer(TimeSpan.FromSeconds(1))
                      .Subscribe(_ => Messenger.RaiseSafe(() =>
                          new WindowActionMessage(WindowAction.Close)));

        }
    }
}