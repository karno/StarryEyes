using System;
using System.Linq;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows.Forms
{
    public class Screen
    {
        /// <summary>
        /// Get all screens.
        /// </summary>
        public static Screen[] AllScreens
        {
            get { return WinForms.Screen.AllScreens.Select(Wrap).ToArray(); }
        }

        /// <summary>
        /// Get primary screen.
        /// </summary>
        public static Screen PrimaryScreen
        {
            get { return Wrap(WinForms.Screen.PrimaryScreen); }
        }

        /// <summary>
        /// Get screen which contains specific window.
        /// </summary>
        /// <param name="handle">Window handle</param>
        public static Screen FromHandle(IntPtr handle)
        {
            return Wrap(WinForms.Screen.FromHandle(handle));
        }

        /// <summary>
        /// Get screen which contains specific point.
        /// </summary>
        /// <param name="pt">point offset, this is only supports integer.</param>
        public static Screen FromPoint(Point pt)
        {
            return Wrap(WinForms.Screen.FromPoint(new System.Drawing.Point((int)pt.X, (int)pt.Y)));
        }

        private static Screen Wrap(WinForms.Screen screen)
        {
            if (screen == null)
                return null;
            else
                return new Screen(screen);
        }

        private readonly WinForms.Screen _original;

        private Screen(WinForms.Screen wfScreen)
        {
            if (wfScreen == null)
                throw new ArgumentNullException("wfScreen");
            this._original = wfScreen;
        }

        /// <summary>
        /// Bits per pixel of this screen.
        /// </summary>
        public int BitsPerPixel
        {
            get { return this._original.BitsPerPixel; }
        }

        /// <summary>
        /// Bounds of this screen.
        /// </summary>
        public Rect Bounds
        {
            get
            {
                return new Rect(
                      this._original.Bounds.Left,
                      this._original.Bounds.Top,
                      this._original.Bounds.Width,
                      this._original.Bounds.Bottom);
            }
        }

        /// <summary>
        /// Working area of this screen.
        /// </summary>
        public Rect WorkingArea
        {
            get
            {
                return new Rect(
                      this._original.WorkingArea.X,
                      this._original.WorkingArea.Y,
                      this._original.WorkingArea.Width,
                      this._original.WorkingArea.Height);
            }
        }

        /// <summary>
        /// Device name of this screen.
        /// </summary>
        public string DeviceName
        {
            get { return this._original.DeviceName; }
        }

        /// <summary>
        /// Whether this screen is primary or not.
        /// </summary>
        public bool IsPrimary
        {
            get { return this._original.Primary; }
        }
    }
}
