using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows
{
    public sealed class ClipboardWatcher : IDisposable
    {
        private bool _isDisposed;

        private bool _isWatching;

        public event EventHandler ClipboardChanged;

        private readonly ClipboardShadowWatcher _watcher;

        public ClipboardWatcher()
        {
            _watcher = new ClipboardShadowWatcher();
            _watcher.DrawClipboard += () =>
            {
                var handler = ClipboardChanged;
                if (handler != null)
                {
                    Task.Run(() => handler(this, EventArgs.Empty));
                }
            };
        }

        public bool IsWatching
        {
            get { return this._isWatching; }
        }

        public void StartWatching()
        {
            if (this.IsWatching) return;
            this._isWatching = true;
            _watcher.StartWatching();
        }

        public void StopWatching()
        {
            if (!this.IsWatching) return;
            this._isWatching = false;
            _watcher.StopWatching();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~ClipboardWatcher()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;
            if (disposing)
            {
                StopWatching();
            }
        }

        private class ClipboardShadowWatcher : Form
        {
            private IntPtr nextHandle = IntPtr.Zero;
            public event Action DrawClipboard;

            public void StartWatching()
            {
                if (nextHandle != IntPtr.Zero) return;
                nextHandle = WinApi.SetClipboardViewer(this.Handle);
            }

            public void StopWatching()
            {
                if (nextHandle == IntPtr.Zero) return;
                WinApi.ChangeClipboardChain(this.Handle, nextHandle);
                nextHandle = IntPtr.Zero;
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case WinApi.WM_DRAWCLIPBOARD:
                        WinApi.SendMessage(nextHandle, m.Msg, m.WParam, m.LParam);
                        var handler = DrawClipboard;
                        if (handler != null)
                        {
                            handler();
                        }
                        break;
                    case WinApi.WM_CHANGECBCHAIN:
                        if (m.WParam == nextHandle)
                        {
                            nextHandle = m.LParam;
                        }
                        else
                        {
                            WinApi.SendMessage(nextHandle, m.Msg, m.WParam, m.LParam);
                        }
                        break;
                }
                base.WndProc(ref m);
            }

            protected override void Dispose(bool disposing)
            {
                StopWatching();
                base.Dispose(disposing);
            }
        }
    }
}
