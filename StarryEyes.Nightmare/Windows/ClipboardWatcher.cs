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

        public bool IsWatching => _isWatching;

        public void StartWatching()
        {
            if (IsWatching) return;
            _isWatching = true;
            _watcher.StartWatching();
        }

        public void StopWatching()
        {
            if (!IsWatching) return;
            _isWatching = false;
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

        private sealed class ClipboardShadowWatcher : Form
        {
            private IntPtr _nextHandle = IntPtr.Zero;
            public event Action DrawClipboard;

            public void StartWatching()
            {
                if (_nextHandle != IntPtr.Zero) return;
                _nextHandle = NativeMethods.SetClipboardViewer(Handle);
            }

            public void StopWatching()
            {
                if (_nextHandle == IntPtr.Zero) return;
                NativeMethods.ChangeClipboardChain(Handle, _nextHandle);
                _nextHandle = IntPtr.Zero;
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case NativeMethods.WM_DRAWCLIPBOARD:
                        NativeMethods.SendMessage(_nextHandle, m.Msg, m.WParam, m.LParam);
                        DrawClipboard?.Invoke();
                        break;
                    case NativeMethods.WM_CHANGECBCHAIN:
                        if (m.WParam == _nextHandle)
                        {
                            _nextHandle = m.LParam;
                        }
                        else
                        {
                            NativeMethods.SendMessage(_nextHandle, m.Msg, m.WParam, m.LParam);
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