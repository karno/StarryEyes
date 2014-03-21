using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace StarryEyes.Hotfixes
{
    /// <summary>
    /// Hotfixes for the clipboard.
    /// </summary>
    public static class ClipboardEx
    {
        public static BitmapSource GetImage()
        {
            byte[] imgBytes;
            using (var clipboardMs = Clipboard.GetData("DeviceIndependentBitmap") as MemoryStream)
            {
                if (clipboardMs == null) return null;
                imgBytes = new byte[clipboardMs.Length];
                clipboardMs.Read(imgBytes, 0, imgBytes.Length);
            }

            var header = FromByteArray<BITMAPINFOHEADER>(imgBytes);

            var fileHeaderSize = Marshal.SizeOf(typeof(BITMAPFILEHEADER));
            var infoHeaderSize = header.biSize;
            var fileSize = fileHeaderSize + header.biSize + header.biSizeImage;

            var fileHeader = new BITMAPFILEHEADER
            {
                bfType = BITMAPFILEHEADER.BM,
                bfSize = fileSize,
                bfReserved1 = 0,
                bfReserved2 = 0,
                bfOffBits = fileHeaderSize + infoHeaderSize + header.biClrUsed * 4
            };

            var fileHeaderBytes = ToByteArray(fileHeader);

            using (var msBitmap = new MemoryStream())
            {
                msBitmap.Write(fileHeaderBytes, 0, fileHeaderSize);
                msBitmap.Write(imgBytes, 0, imgBytes.Length);
                try
                {
                    return BitmapFrame.Create(msBitmap, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
                catch
                {
                    return null;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        // ReSharper disable once InconsistentNaming
        private struct BITMAPFILEHEADER
        {
            public static readonly short BM = 0x4d42; // BM

            public short bfType;
            public int bfSize;
            public short bfReserved1;
            public short bfReserved2;
            public int bfOffBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        // ReSharper disable once InconsistentNaming
        private struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        public static T FromByteArray<T>(byte[] bytes) where T : struct
        {
            var ptr = IntPtr.Zero;
            try
            {
                var size = Marshal.SizeOf(typeof(T));
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, ptr, size);
                var obj = Marshal.PtrToStructure(ptr, typeof(T));
                return (T)obj;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        public static byte[] ToByteArray<T>(T obj) where T : struct
        {
            var ptr = IntPtr.Zero;
            try
            {
                var size = Marshal.SizeOf(typeof(T));
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, ptr, true);
                var bytes = new byte[size];
                Marshal.Copy(ptr, bytes, 0, size);
                return bytes;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
