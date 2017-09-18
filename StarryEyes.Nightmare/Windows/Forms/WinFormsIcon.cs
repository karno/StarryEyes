using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace StarryEyes.Nightmare.Windows.Forms
{
    /// <summary>
    /// Wrapper of System.Windows.Forms.Icon
    /// </summary>
    public class WinFormsIcon : IDisposable
    {
        internal readonly Icon IconInstance;

        public WinFormsIcon(Stream stream)
        {
            IconInstance = new Icon(stream);
        }

        public WinFormsIcon(string file)
        {
            IconInstance = new Icon(file);
        }

        public WinFormsIcon(BitmapImage image)
        {
            var width = image.PixelWidth;
            var height = image.PixelHeight;
            var stride = width * ((image.Format.BitsPerPixel + 7) / 8);
            var ptr = Marshal.AllocHGlobal(height * stride);
            try
            {
                image.CopyPixels(new Int32Rect(0, 0, width, height), ptr, height * stride, stride);
                using (var bitmap = new Bitmap(width, height, stride, PixelFormat.Format32bppArgb, ptr))
                {
                    IconInstance = Icon.FromHandle(bitmap.GetHicon());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        internal WinFormsIcon(Icon icon)
        {
            IconInstance = icon;
        }

        public void Dispose()
        {
            IconInstance.Dispose();
        }
    }
}