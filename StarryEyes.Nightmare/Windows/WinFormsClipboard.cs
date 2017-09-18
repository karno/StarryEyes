using System.Windows.Media.Imaging;
using WinForms = System.Windows.Forms;

namespace StarryEyes.Nightmare.Windows
{
    public static class WinFormsClipboard
    {
        public static BitmapSource GetWpfImage()
        {
            return WinForms.Clipboard.GetImage().ToWpfBitmap();
        }
    }
}