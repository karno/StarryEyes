using System;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows.Forms
{
    /// <summary>
    /// System.Windows.Forms.MouseEventHandlerのラッパ実装です。
    /// </summary>
    public delegate void WinFormsMouseEventHandler(object sender, WinFormsMouseEventArgs e);

    /// <summary>
    /// System.Windows.Forms.MouseEventArgsのWPFラッパ実装です。
    /// </summary>
    public class WinFormsMouseEventArgs : EventArgs
    {
        private WinForms.MouseEventArgs eventArgs;

        public WinFormsMouseEventArgs(WinForms.MouseEventArgs e)
        {
            this.eventArgs = e;
        }

        /// <summary>
        /// マウスのどのボタンが押されたかを示す値を取得します。 
        /// </summary>
        public MouseButtons Button
        {
            get
            {
                var mb = MouseButtons.None;
                if (this.eventArgs.Button.HasFlag(WinForms.MouseButtons.Left))
                    mb |= MouseButtons.Left;
                if (this.eventArgs.Button.HasFlag(WinForms.MouseButtons.Middle))
                    mb |= MouseButtons.Middle;
                if (this.eventArgs.Button.HasFlag(WinForms.MouseButtons.Right))
                    mb |= MouseButtons.Right;
                if (this.eventArgs.Button.HasFlag(WinForms.MouseButtons.XButton1))
                    mb |= MouseButtons.XButton1;
                if (this.eventArgs.Button.HasFlag(WinForms.MouseButtons.XButton2))
                    mb |= MouseButtons.XButton2;
                return mb;
            }
        }

        /// <summary>
        /// マウス ボタンが押されて離された回数を取得します。 
        /// </summary>
        public int Clicks
        {
            get { return this.eventArgs.Clicks; }
        }

        /// <summary>
        /// マウス ホイールの回転回数を表す符合付きの数値を取得します。マウス ホイールのノッチ 1 つ分が 1 移動量に相当します。 
        /// </summary>
        public int Delta
        {
            get { return this.eventArgs.Delta; }
        }

        /// <summary>
        /// マウス イベント生成時のマウスの位置を取得します。(System.Windows.Pointに変換されています。)
        /// </summary>
        public Point Location
        {
            get { return new Point(this.eventArgs.X, this.eventArgs.Y); }
        }

        /// <summary>
        /// マウス イベント生成時のマウスの x 座標を取得します。 
        /// </summary>
        public int X
        {
            get { return this.eventArgs.X; }
        }

        /// <summary>
        /// マウス イベント生成時のマウスの y 座標を取得します。 
        /// </summary>
        public int Y
        {
            get { return this.eventArgs.Y; }
        }
    }

    [Flags()]
    public enum MouseButtons
    {
        None = 0,
        Left = 1,
        Middle = 2,
        Right = 4,
        XButton1 = 8,
        XButton2 = 16,
    }
}
