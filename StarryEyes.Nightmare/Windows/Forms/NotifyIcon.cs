using System;
using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows.Forms
{
    /// <summary>
    /// WPF wrapper of System.Windows.Forms.NotifyIcon
    /// </summary>
    public class NotifyIcon : IDisposable
    {
        private WinForms.NotifyIcon notifyIcon;

        public NotifyIcon()
        {
            this.notifyIcon = new WinForms.NotifyIcon();
            // イベントの接続
            this.notifyIcon.BalloonTipClicked += (_, e) =>
            {
                var btc = this.BalloonTipClicked;
                if (btc != null)
                    btc(this, e);
            };
            this.notifyIcon.BalloonTipClosed += (_, e) =>
            {
                var btc = this.BalloonTipClosed;
                if (btc != null)
                    btc(this, e);
            };
            this.notifyIcon.BalloonTipShown += (_, e) =>
            {
                var bts = this.BalloonTipShown;
                if (bts != null)
                    bts(this, e);
            };

            this.notifyIcon.MouseClick += (_, e) =>
            {
                var mc = this.MouseClick;
                if (mc != null)
                {
                    mc(this, new WinFormsMouseEventArgs(e));
                }
            };
            this.notifyIcon.MouseDoubleClick += (_, e) =>
            {
                var mdc = this.MouseDoubleClick;
                if (mdc != null)
                {
                    mdc(this, new WinFormsMouseEventArgs(e));
                }
            };
            this.notifyIcon.MouseDown += (_, e) =>
            {
                var md = this.MouseDown;
                if (md != null)
                {
                    md(this, new WinFormsMouseEventArgs(e));
                }
            };
            this.notifyIcon.MouseMove += (_, e) =>
            {
                var mm = this.MouseMove;
                if (mm != null)
                {
                    mm(this, new WinFormsMouseEventArgs(e));
                }
            };
            this.notifyIcon.MouseUp += (_, e) =>
            {
                var mu = this.MouseUp;
                if (mu != null)
                {
                    mu(this, new WinFormsMouseEventArgs(e));
                }
            };
        }

        /// <summary>
        /// NotifyIcon に関連付けられているバルーン ヒントに表示するアイコンを取得または設定します。 
        /// </summary>
        public NotifyIconToolTipIcon BalloonTipIcon
        {
            get
            {
                switch (this.notifyIcon.BalloonTipIcon)
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
                        this.notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Error;
                        break;
                    case NotifyIconToolTipIcon.Info:
                        this.notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Info;
                        break;
                    case NotifyIconToolTipIcon.Warning:
                        this.notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Warning;
                        break;
                    default:
                        this.notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.None;
                        break;
                }
            }
        }

        /// <summary>
        /// NotifyIcon に関連付けられているバルーン ヒントに表示するテキストを取得または設定します。 
        /// </summary>
        public string BalloonTipText
        {
            get { return this.notifyIcon.BalloonTipText; }
            set { this.notifyIcon.BalloonTipText = value; }
        }

        /// <summary>
        /// NotifyIcon で表示されるバルーン ヒントのタイトルを取得または設定します。 
        /// </summary>
        public string BalloonTipTitle
        {
            get { return this.notifyIcon.BalloonTipTitle; }
            set { this.notifyIcon.BalloonTipTitle = value; }
        }

        /// <summary>
        /// マウス ポインタが通知領域アイコンの上にあるときに表示されるツールヒント テキストを取得または設定します。 
        /// </summary>
        public string Text
        {
            get { return this.notifyIcon.Text; }
            set { this.notifyIcon.Text = value; }
        }

        /// <summary>
        /// アイコンをタスクバーの通知領域に表示するかどうかを示す値を取得または設定します。 
        /// </summary>
        public bool IsVisible
        {
            get { return this.notifyIcon.Visible; }
            set { this.notifyIcon.Visible = value; }
        }

        public WinFormsIcon Icon
        {
            get { return new WinFormsIcon(this.notifyIcon.Icon); }
            set { this.notifyIcon.Icon = value.IconInstance; }
        }

        /// <summary>
        /// バルーン ヒントがクリックされたときに発生します。 
        /// </summary>
        public event EventHandler BalloonTipClicked;

        /// <summary>
        /// ユーザーがバルーン ヒントを閉じたときに発生します。 
        /// </summary>
        public event EventHandler BalloonTipClosed;

        /// <summary>
        /// 画面にバルーン ヒントが表示されたときに発生します。 
        /// </summary>
        public event EventHandler BalloonTipShown;

        /// <summary>
        /// ユーザーがマウスで NotifyIcon をクリックすると発生します。 
        /// </summary>
        public event WinFormsMouseEventHandler MouseClick;

        /// <summary>
        /// ユーザーがマウスで NotifyIcon をダブルクリックすると発生します。 
        /// </summary>
        public event WinFormsMouseEventHandler MouseDoubleClick;

        /// <summary>
        /// ポインタがタスクバーの通知領域のアイコンの上にあるときに、マウス ボタンを押すと発生します。 
        /// </summary>
        public event WinFormsMouseEventHandler MouseDown;

        /// <summary>
        /// ポインタがタスクバーの通知領域のアイコンの上にあるときに、マウスを移動すると発生します。 
        /// </summary>
        public event WinFormsMouseEventHandler MouseMove;

        /// <summary>
        /// ポインタがタスクバーの通知領域のアイコンの上にあるときに、マウス ボタンを離すと発生します。 
        /// </summary>
        public event WinFormsMouseEventHandler MouseUp;

        /// <summary>
        /// リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            notifyIcon.Dispose();
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
