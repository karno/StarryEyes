using System.Windows;

namespace StarryEyes.Views.Utils
{
    public class StringToVisibleConverter : OneWayConverter<object, Visibility>
    {
        public override Visibility ToTarget(object input, object parameter)
        {
            return input.ToString() == (parameter as string) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
