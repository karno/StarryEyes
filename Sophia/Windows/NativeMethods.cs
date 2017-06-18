using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

// ReSharper disable InconsistentNaming

namespace Sophia.Windows
{
    internal static class NativeMethods
    {
        internal const int GWL_EXSTYLE = -20;

        internal static Rect GetWindowPlacement(this Window window)
        {
            var helper = new WindowInteropHelper(window);
            var wpl = new WINDOWPLACEMENT();
            wpl.length = Marshal.SizeOf(wpl);
            GetWindowPlacement(helper.Handle, ref wpl);
            var width = wpl.rcNormalPosition.right - wpl.rcNormalPosition.left;
            var height = wpl.rcNormalPosition.bottom - wpl.rcNormalPosition.top;
            return new Rect(wpl.rcNormalPosition.left, wpl.rcNormalPosition.top,
                width >= 0 ? width : 0,
                height >= 0 ? height : 0);
        }

        internal static void SetWindowPlacement(this Window window, Rect placement)
        {
            var helper = new WindowInteropHelper(window);
            var wpl = new WINDOWPLACEMENT();
            wpl.length = Marshal.SizeOf(wpl);
            wpl.rcNormalPosition.left = (int)placement.Left;
            wpl.rcNormalPosition.right = (int)placement.Right;
            wpl.rcNormalPosition.top = (int)placement.Top;
            wpl.rcNormalPosition.bottom = (int)placement.Bottom;
            SetWindowPlacement(helper.Handle, ref wpl);
        }

        internal static int GetWindowExStyle(this Window window)
        {
            var helper = new WindowInteropHelper(window);
            return (int)GetWindowLongPtr(helper.Handle, GWL_EXSTYLE);
        }

        internal static void SetWindowExStyle(this Window window, int style)
        {
            var helper = new WindowInteropHelper(window);
            SetWindowLongPtr(helper.Handle, GWL_EXSTYLE, (IntPtr)style);
        }

        internal static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return Environment.Is64BitProcess
                ? GetWindowLong64(hWnd, nIndex)
                : GetWindowLong32(hWnd, nIndex);
        }

        internal static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return Environment.Is64BitProcess
                ? SetWindowLong64(hWnd, nIndex, dwNewLong)
                : SetWindowLong32(hWnd, nIndex, dwNewLong);
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLong64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLong64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpWndPl);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpWndPl);
    }


    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int x;
        public int y;
        public POINT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
        public RECT(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }
}