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
            DispatcherHolder.Enqueue(() => new DisplayMarkerWindow
            {
                DataContext = new DisplayMarkerViewModel(displayIndex)
            }.Show());
        }

        private readonly int _left;
        private readonly int _top;

        public int Left
        {
            get { return this._left; }
        }

        public int Top
        {
            get { return this._top; }
        }

        private DisplayMarkerViewModel(int displayIndex)
        {
            var screen = NotificationUtil.GetScreenOfIndex(displayIndex);
            if (screen == null) return;
            var warea = screen.WorkingArea;
            if (warea.Width < 1 || warea.Height < 1) return;
            this._left = (int)((warea.Width - 300) / 2 + warea.Left);
            this._top = (int)((warea.Height - 60) / 2 + warea.Top);
        }

        public void Shown()
        {
            Observable.Timer(TimeSpan.FromSeconds(1))
                      .Subscribe(_ => this.Messenger.RaiseSafe(() =>
                          new WindowActionMessage(WindowAction.Close)));

        }
    }
}
