using System;
using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows.Forms
{
    /// <summary>
    /// WPF wrapper of System.Windows.Forms.NotifyIcon
    /// </summary>
    public class NotifyIcon : IDisposable
    {
        private readonly WinForms.NotifyIcon _notifyIcon;

        public NotifyIcon()
        {
            _notifyIcon = new WinForms.NotifyIcon();
            _notifyIcon.BalloonTipClicked += (_, e) => BalloonTipClicked?.Invoke(this, e);
            _notifyIcon.BalloonTipClosed += (_, e) => BalloonTipClosed?.Invoke(this, e);
            _notifyIcon.BalloonTipShown += (_, e) => BalloonTipShown?.Invoke(this, e);

            _notifyIcon.MouseClick += (_, e) => MouseClick?.Invoke(this, new WinFormsMouseEventArgs(e));
            _notifyIcon.MouseDoubleClick += (_, e) => MouseDoubleClick?.Invoke(this, new WinFormsMouseEventArgs(e));
            _notifyIcon.MouseDown += (_, e) => MouseDown?.Invoke(this, new WinFormsMouseEventArgs(e));
            _notifyIcon.MouseMove += (_, e) => MouseMove?.Invoke(this, new WinFormsMouseEventArgs(e));
            _notifyIcon.MouseUp += (_, e) => MouseUp?.Invoke(this, new WinFormsMouseEventArgs(e));
        }

        /// <summary>
        /// Gets or sets the icon to display in the balloon hint associated with the notify icon.
        /// </summary>
        public NotifyIconToolTipIcon BalloonTipIcon
        {
            get
            {
                switch (_notifyIcon.BalloonTipIcon)
                {
                    case WinForms.ToolTipIcon.Error:
                        return NotifyIconToolTipIcon.Error;
                    case WinForms.ToolTipIcon.Info:
                        return NotifyIconToolTipIcon.Info;
                    case WinForms.ToolTipIcon.Warning:
                        return NotifyIconToolTipIcon.Warning;
                    default:
                        return NotifyIconToolTipIcon.None;
                }
            }
            set
            {
                switch (value)
                {
                    case NotifyIconToolTipIcon.Error:
                        _notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Error;
                        break;
                    case NotifyIconToolTipIcon.Info:
                        _notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Info;
                        break;
                    case NotifyIconToolTipIcon.Warning:
                        _notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Warning;
                        break;
                    default:
                        _notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.None;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or sets the text to display in the balloon hint associated with the notify icon.
        /// </summary>
        public string BalloonTipText
        {
            get => _notifyIcon.BalloonTipText;
            set => _notifyIcon.BalloonTipText = value;
        }

        /// <summary>
        /// Gets or sets the title of the balloon hint displayed in the notify icon.
        /// </summary>
        public string BalloonTipTitle
        {
            get => _notifyIcon.BalloonTipTitle;
            set => _notifyIcon.BalloonTipTitle = value;
        }

        /// <summary>
        /// Gets or sets the tool-tip text to be displayed when the mouse pointer is over the notify icon.
        /// </summary>
        public string Text
        {
            get => _notifyIcon.Text;
            set => _notifyIcon.Text = value;
        }

        /// <summary>
        /// Gets or sets the visibility of the notify icon.
        /// </summary>
        public bool IsVisible
        {
            get => _notifyIcon.Visible;
            set => _notifyIcon.Visible = value;
        }

        /// <summary>
        /// Gets or sets of the notify icon that displayed on notification area.
        /// </summary>
        public WinFormsIcon Icon
        {
            get => new WinFormsIcon(_notifyIcon.Icon);
            set => _notifyIcon.Icon = value.IconInstance;
        }

        /// <summary>
        /// Occurs when the balloon hint is clicked.
        /// </summary>
        public event EventHandler BalloonTipClicked;

        /// <summary>
        /// Occurs when the balloon hint is closed.
        /// </summary>
        public event EventHandler BalloonTipClosed;

        /// <summary>
        /// Occurs when the balloon hint is shown.
        /// </summary>
        public event EventHandler BalloonTipShown;

        /// <summary>
        /// Occurs when the user clicking the notify icon.
        /// </summary>
        public event WinFormsMouseEventHandler MouseClick;

        /// <summary>
        /// Occurs when the user double-clicking the notify icon.
        /// </summary>
        public event WinFormsMouseEventHandler MouseDoubleClick;

        /// <summary>
        /// Occurs when the mouse button is pressed while the mouse pointer is over the notify icon.
        /// </summary>
        public event WinFormsMouseEventHandler MouseDown;

        /// <summary>
        /// Occurs when the mouse cursor is moved while the mouse pointer is over the notify icon.
        /// </summary>
        public event WinFormsMouseEventHandler MouseMove;

        /// <summary>
        /// Occurs when the mouse button is released while the mouse pointer is over the notify icon.
        /// </summary>
        public event WinFormsMouseEventHandler MouseUp;

        /// <summary>
        /// Release all resources.
        /// </summary>
        public void Dispose()
        {
            _notifyIcon.Dispose();
        }
    }

    public enum NotifyIconToolTipIcon
    {
        Error,
        Info,
        None,
        Warning,
    }
}