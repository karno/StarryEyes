using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StarryEyes.Nightmare.Windows
{
    public static class WindowNativeExtension
    {
        public static Rect GetWindowPlacement(this Window window)
        {
            var helper = new WindowInteropHelper(window);
            var wpl = new WINDOWPLACEMENT();
            wpl.length = Marshal.SizeOf(wpl);
            NativeMethods.GetWindowPlacement(helper.Handle, ref wpl);
            var width = wpl.rcNormalPosition.right - wpl.rcNormalPosition.left;
            var height = wpl.rcNormalPosition.bottom - wpl.rcNormalPosition.top;
            return new Rect(wpl.rcNormalPosition.left, wpl.rcNormalPosition.top,
                width >= 0 ? width : 0,
                height >= 0 ? height : 0);
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
            NativeMethods.SetWindowPlacement(helper.Handle, ref wpl);
        }

        public static int GetWindowExStyle(this Window window)
        {
            var helper = new WindowInteropHelper(window);
            return (int)NativeMethods.GetWindowLongPtr(helper.Handle, NativeMethods.GWL_EXSTYLE);
        }

        public static void SetWindowExStyle(this Window window, int style)
        {
            var helper = new WindowInteropHelper(window);
            NativeMethods.SetWindowLongPtr(helper.Handle, NativeMethods.GWL_EXSTYLE, (IntPtr)style);
        }
    }
}