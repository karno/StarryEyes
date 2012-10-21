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
            get
            {
                var all = WinForms.Screen.AllScreens;
                if (all == null)
                    return null;
                else
                    return all.Select(s => Wrap(s)).ToArray();
            }
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

        readonly WinForms.Screen original;

        private Screen(WinForms.Screen wfScreen)
        {
            if (wfScreen == null)
                throw new ArgumentNullException("wfScreen");
            this.original = wfScreen;
        }

        /// <summary>
        /// Bits per pixel of this screen.
        /// </summary>
        public int BitsPerPixel
        {
            get { return this.original.BitsPerPixel; }
        }

        /// <summary>
        /// Bounds of this screen.
        /// </summary>
        public Rect Bounds
        {
            get
            {
                return new Rect(
                      this.original.Bounds.Left,
                      this.original.Bounds.Top,
                      this.original.Bounds.Width,
                      this.original.Bounds.Bottom);
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
                      this.original.WorkingArea.Left,
                      this.original.WorkingArea.Top,
                      this.original.WorkingArea.Width,
                      this.original.WorkingArea.Bottom);
            }
        }

        /// <summary>
        /// Device name of this screen.
        /// </summary>
        public string DeviceName
        {
            get { return this.original.DeviceName; }
        }

        /// <summary>
        /// Whether this screen is primary or not.
        /// </summary>
        public bool IsPrimary
        {
            get { return this.original.Primary; }
        }
    }
}
