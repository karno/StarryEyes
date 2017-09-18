using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SDI = System.Drawing.Imaging;

namespace StarryEyes.Nightmare.Windows
{
    public static class ImageInterop
    {
        public static BitmapSource ToWpfBitmap(this Image image)
        {
            using (var bmp = new Bitmap(image.Width, image.Height, SDI.PixelFormat.Format32bppArgb))
            {
                // write to bitmap
                using (var g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(image, new Point());
                }

                // prepare values
                var width = bmp.Width;
                var height = bmp.Height;
                var stride = bmp.Width * 4;

                // prepare buffer
                var buffer = new byte[stride * bmp.Height];

                SDI.BitmapData bd = null;
                try
                {
                    // copy pixels
                    bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                        SDI.ImageLockMode.ReadOnly, SDI.PixelFormat.Format32bppArgb);
                    Marshal.Copy(bd.Scan0, buffer, 0, buffer.Length);
                }
                finally
                {
                    if (bd != null)
                    {
                        bmp.UnlockBits(bd);
                    }
                }

                // create image from byte buffer
                return BitmapSource.Create(width, height, 96d, 96d, PixelFormats.Bgra32,
                    null, buffer, stride);
            }
        }

        public static Bitmap ToGdiBitmap(BitmapSource bmp)
        {
            // below code refernces blog entry by luche:
            // http://www.ruche-home.net/%A4%DC%A4%E4%A4%AD%A4%B4%A4%C8/2011-08-02/CopyPixels%20%A5%E1%A5%BD%A5%C3%A5%C9%A4%F2%CD%D1%A4%A4%A4%BF%20WPF%20BitmapSource%20%A4%AB%A4%E9%20GDI%20Bitmap%20%A4%D8%A4%CE%CA%D1%B4%B9

            if (bmp.Format != PixelFormats.Bgra32)
            {
                // convert format
                bmp = new FormatConvertedBitmap(bmp,
                    PixelFormats.Bgra32, null, 0);
                bmp.Freeze();
            }

            // prepare values
            var width = (int)bmp.Width;
            var height = (int)bmp.Height;
            var stride = width * 4;

            // prepare buffer
            var buffer = new byte[stride * height];

            // copy to byte array
            bmp.CopyPixels(buffer, stride, 0);

            // create bitmap
            var result = new Bitmap(width, height, SDI::PixelFormat.Format32bppArgb);

            // set bitmap content
            try
            {
                // locking bits
                SDI::BitmapData bd = null;
                try
                {
                    bd = result.LockBits(new Rectangle(0, 0, width, height),
                        SDI::ImageLockMode.WriteOnly, SDI::PixelFormat.Format32bppArgb);
                    Marshal.Copy(buffer, 0, bd.Scan0, buffer.Length);
                }
                finally
                {
                    if (bd != null)
                    {
                        result.UnlockBits(bd);
                    }
                }
            }
            catch
            {
                result.Dispose();
                throw;
            }

            return result;
        }
    }
}