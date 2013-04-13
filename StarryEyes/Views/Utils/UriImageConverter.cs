using System;
using System.Windows.Media.Imaging;

namespace StarryEyes.Views.Utils
{
    public class UriImageConverter : OneWayConverter<Uri, BitmapImage>
    {
        protected override BitmapImage ToTarget(Uri input, object parameter)
        {
            if (input == null)
                return null;
            try
            {
                return new BitmapImage(input)
                {
                    CacheOption = BitmapCacheOption.OnDemand,
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
