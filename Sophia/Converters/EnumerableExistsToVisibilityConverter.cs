using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Sophia.Converters
{
    public class EnumerableExistsToVisibilityConverter : OneWayConverter<IEnumerable, Visibility>
    {
        protected override Visibility ToTarget(IEnumerable input, object parameter, CultureInfo culture)
        {
            var exist = false;
            if (input is IList list)
            {
                exist = list.Count > 0;
            }
            else if (input != null)
            {
                exist = input.Cast<object>().Any();
            }
            return exist ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class InverseEnumerableExistsToVisibilityConverter : OneWayConverter<IEnumerable, Visibility>
    {
        protected override Visibility ToTarget(IEnumerable input, object parameter, CultureInfo culture)
        {
            var exist = false;
            if (input is IList list)
            {
                exist = list.Count > 0;
            }
            else if (input != null)
            {
                exist = input.Cast<object>().Any();
            }
            return exist ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}