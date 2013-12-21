using System;
using System.ComponentModel;
using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows
{
    public static class SystemInformation
    {
        public static string ComputerName
        {
            get { return WinForms.SystemInformation.ComputerName; }
        }

        public static string UserName
        {
            get { return WinForms.SystemInformation.UserName; }
        }

        public static bool MouseButtonsSwapped
        {
            get { return WinForms.SystemInformation.MouseButtonsSwapped; }
        }

        public static int MouseWheelScrollDelta
        {
            get { return WinForms.SystemInformation.MouseWheelScrollDelta; }
        }

        public static uint DesktopHeapSize
        {
            get
            {
                var hDesktop = WinApi.OpenInputDesktop(0, false, WinApi.GENERIC_READ);
                if (hDesktop == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
                try
                {
                    var buffer = new byte[4]; // unsigned long(32bit)
                    uint n;
                    if (!WinApi.GetUserObjectInformation(hDesktop, WinApi.UOI_HEAPSIZE, buffer, sizeof(byte) * 4, out n))
                    {
                        throw new Win32Exception();
                    }
                    return BitConverter.ToUInt32(buffer, 0);
                }
                finally
                {
                    WinApi.CloseDesktop(hDesktop);
                }
            }
        }
    }
}
