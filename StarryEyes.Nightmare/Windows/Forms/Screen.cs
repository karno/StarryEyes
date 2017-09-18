using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using Point = System.Windows.Point;
using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows.Forms
{
    public class Screen
    {
        private static readonly Version Windows8 = new Version(6, 2, 9200, 0);

        /// <summary>
        /// Get all screens.
        /// </summary>
        public static Screen[] AllScreens => WinForms.Screen.AllScreens.Select(Wrap).ToArray();

        /// <summary>
        /// Get primary screen.
        /// </summary>
        public static Screen PrimaryScreen => Wrap(WinForms.Screen.PrimaryScreen);

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
            return screen == null ? null : new Screen(screen);
        }

        private readonly WinForms.Screen _original;
        private readonly uint _dpiX;
        private readonly uint _dpiY;

        private Screen(WinForms.Screen wfScreen)
        {
            _original = wfScreen ?? throw new ArgumentNullException(nameof(wfScreen));

            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                Environment.OSVersion.Version >= Windows8)
            {
                var monitor = NativeMethods.MonitorFromPoint(
                    new System.Drawing.Point((int)WorkingArea.Left, (int)WorkingArea.Top), 2);
                NativeMethods.GetDpiForMonitor(monitor, DpiType.Effective, out _dpiX, out _dpiY);
            }
            else
            {
                var g = Graphics.FromHwnd(IntPtr.Zero);
                var desktop = g.GetHdc();

                _dpiX = (uint)NativeMethods.GetDeviceCaps(desktop, (int)DeviceCap.LOGPIXELSX);
                _dpiY = (uint)NativeMethods.GetDeviceCaps(desktop, (int)DeviceCap.LOGPIXELSY);
            }
        }

        /// <summary>
        /// Bits per pixel of this screen.
        /// </summary>
        public int BitsPerPixel => _original.BitsPerPixel;

        /// <summary>
        /// Horizontal DPI (base: 96)
        /// </summary>
        public uint DpiX => _dpiX;

        /// <summary>
        /// Vertical DPI (base: 96)
        /// </summary>
        public uint DpiY => _dpiY;

        /// <summary>
        /// Bounds of this screen.
        /// </summary>
        public Rect Bounds => new Rect(
            _original.Bounds.Left,
            _original.Bounds.Top,
            _original.Bounds.Width,
            _original.Bounds.Bottom);

        /// <summary>
        /// Working area of this screen.
        /// </summary>
        public Rect WorkingArea => new Rect(
            _original.WorkingArea.X,
            _original.WorkingArea.Y,
            _original.WorkingArea.Width,
            _original.WorkingArea.Height);

        /// <summary>
        /// Device name of this screen.
        /// </summary>
        public string DeviceName => _original.DeviceName;

        /// <summary>
        /// Whether this screen is primary or not.
        /// </summary>
        public bool IsPrimary => _original.Primary;
    }
}