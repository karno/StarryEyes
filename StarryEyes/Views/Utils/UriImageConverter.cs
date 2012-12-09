using System;
using System.Windows.Media.Imaging;

namespace StarryEyes.Views.Utils
{
    public class UriImageConverter : OneWayConverter<Uri, BitmapImage>
    {
        public override BitmapImage ToTarget(Uri input, object parameter)
        {
            if (input == null)
                return null;
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnDemand;
                image.CreateOptions = BitmapCreateOptions.DelayCreation;
                image.UriSource = input;
                image.EndInit();
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
