using System;
using System.ComponentModel;
using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows
{
    public static class SystemInformation
    {
        public static string ComputerName => WinForms.SystemInformation.ComputerName;

        public static string UserName => WinForms.SystemInformation.UserName;

        public static bool MouseButtonsSwapped => WinForms.SystemInformation.MouseButtonsSwapped;

        public static int MouseWheelScrollDelta => WinForms.SystemInformation.MouseWheelScrollDelta;

        public static uint DesktopHeapSize
        {
            get
            {
                var hDesktop = NativeMethods.OpenInputDesktop(0, false, NativeMethods.GENERIC_READ);
                if (hDesktop == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
                try
                {
                    var buffer = new byte[4]; // unsigned long(32bit)
                    if (!NativeMethods.GetUserObjectInformation(hDesktop, NativeMethods.UOI_HEAPSIZE, buffer,
                        sizeof(byte) * 4, out uint _))
                    {
                        throw new Win32Exception();
                    }
                    return BitConverter.ToUInt32(buffer, 0);
                }
                finally
                {
                    NativeMethods.CloseDesktop(hDesktop);
                }
            }
        }
    }
}