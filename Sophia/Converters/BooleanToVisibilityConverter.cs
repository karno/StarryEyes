using System.Globalization;
using System.Windows;

namespace Sophia.Converters
{
    public class BooleanToVisibilityConverter : TwoWayConverter<bool, Visibility>
    {
        protected override Visibility ToTarget(bool input, object parameter, CultureInfo culture)
        {
            return input ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override bool ToSource(Visibility input, object parameter, CultureInfo culture)
        {
            return input == Visibility.Visible;
        }
    }

    public class InverseBooleanToVisibilityConverter : TwoWayConverter<bool, Visibility>
    {
        protected override Visibility ToTarget(bool input, object parameter, CultureInfo culture)
        {
            return input ? Visibility.Collapsed : Visibility.Visible;
        }

        protected override bool ToSource(Visibility input, object parameter, CultureInfo culture)
        {
            return input == Visibility.Collapsed;
        }
    }
}