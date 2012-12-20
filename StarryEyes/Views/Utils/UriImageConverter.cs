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
                return new BitmapImage(input)
                {
                    CacheOption = BitmapCacheOption.OnDemand,
                    CreateOptions = BitmapCreateOptions.DelayCreation,
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Image resolve failed: " + ex.Message);
                return null;
            }
        }
    }
}
