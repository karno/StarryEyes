using System;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows.Forms
{
    /// <summary>
    /// Wrapper of System.Windows.Forms.MouseEventHandler
    /// </summary>
    public delegate void WinFormsMouseEventHandler(object sender, WinFormsMouseEventArgs e);

    /// <summary>
    /// Wrapper of System.Windows.Forms.MouseEventArgs
    /// </summary>
    public class WinFormsMouseEventArgs : EventArgs
    {
        private readonly WinForms.MouseEventArgs _eventArgs;

        public WinFormsMouseEventArgs(WinForms.MouseEventArgs e)
        {
            _eventArgs = e;
        }

        /// <summary>
        /// Get the value of which button is pressed on the mouse.
        /// </summary>
        public MouseButtons Button
        {
            get
            {
                var mb = MouseButtons.None;
                if (_eventArgs.Button.HasFlag(WinForms.MouseButtons.Left))
                    mb |= MouseButtons.Left;
                if (_eventArgs.Button.HasFlag(WinForms.MouseButtons.Middle))
                    mb |= MouseButtons.Middle;
                if (_eventArgs.Button.HasFlag(WinForms.MouseButtons.Right))
                    mb |= MouseButtons.Right;
                if (_eventArgs.Button.HasFlag(WinForms.MouseButtons.XButton1))
                    mb |= MouseButtons.XButton1;
                if (_eventArgs.Button.HasFlag(WinForms.MouseButtons.XButton2))
                    mb |= MouseButtons.XButton2;
                return mb;
            }
        }

        /// <summary>
        /// Get the counts of clicking the mouse button.
        /// </summary>
        public int Clicks => _eventArgs.Clicks;

        /// <summary>
        /// Get the value of wheel rotation. This property returns a signed value.
        /// One notch of the mouse wheel corresponds to one movement amount.
        /// </summary>
        public int Delta => _eventArgs.Delta;

        /// <summary>
        /// Get the position of the mouse when this event occurred.
        /// </summary>
        public Point Location => new Point(_eventArgs.X, _eventArgs.Y);

        /// <summary>
        /// Gets the X position  of the mouse when this event occurred.
        /// </summary>
        public int X => _eventArgs.X;

        /// <summary>
        /// Gets the Y position  of the mouse when this event occurred.
        /// </summary>
        public int Y => _eventArgs.Y;
    }

    [Flags]
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