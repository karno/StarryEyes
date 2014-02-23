using System;
using System.Runtime.InteropServices;

namespace StarryEyes.Nightmare.Windows
{
    // ReSharper disable InconsistentNaming
    internal static class WinApi
    {
        // constants
        internal const uint GENERIC_READ = 0x80000000;
        internal const int UOI_HEAPSIZE = 5;
        internal const int WM_DRAWCLIPBOARD = 0x0308;
        internal const int WM_CHANGECBCHAIN = 0x030D;
        internal const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit,
          uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex,
          [Out] byte[] pvInfo, uint nLength, out uint lpnLengthNeeded);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        internal extern static bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpWndPl);

        [DllImport("user32.dll")]
        internal extern static bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpWndPl);

        [DllImport("user32.dll")]
        internal static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetClipboardViewer(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ChangeClipboardChain(IntPtr hWnd, IntPtr hWndNext);
    }

    #region Data structures

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

    #endregion
    // ReSharper restore InconsistentNaming
}
