using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StarryEyes.Nightmare.Windows
{
    public static class WindowPlacements
    {
        public static Rect GetWindowPlacement(this Window window)
        {
            var helper = new WindowInteropHelper(window);
            var wpl = new WINDOWPLACEMENT();
            wpl.length = Marshal.SizeOf(wpl);
            WinApi.GetWindowPlacement((int)helper.Handle, ref wpl);
            return new Rect(wpl.rcNormalPosition.left, wpl.rcNormalPosition.top,
                wpl.rcNormalPosition.right - wpl.rcNormalPosition.left,
                wpl.rcNormalPosition.bottom - wpl.rcNormalPosition.top);
        }

        public static void SetWindowPlacement(this Window window, Rect placement)
        {
            var helper = new WindowInteropHelper(window);
            var wpl = new WINDOWPLACEMENT();
            wpl.length = Marshal.SizeOf(wpl);
            wpl.rcNormalPosition.left = (int)placement.Left;
            wpl.rcNormalPosition.right = (int)placement.Right;
            wpl.rcNormalPosition.top = (int)placement.Top;
            wpl.rcNormalPosition.bottom = (int)placement.Bottom;
            WinApi.SetWindowPlacement((int)helper.Handle, ref wpl);
        }
    }
}