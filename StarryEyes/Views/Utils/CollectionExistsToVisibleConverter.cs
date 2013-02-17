using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace StarryEyes.Views.Utils
{
    public class CollectionExistsToVisibleConverter : OneWayConverter<IEnumerable<object>, Visibility>
    {
        protected override Visibility ToTarget(IEnumerable<object> input, object parameter)
        {
            return input != null && input.Any() ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class CollectionExistsToInvisibleConverter : OneWayConverter<IEnumerable<object>, Visibility>
    {
        protected override Visibility ToTarget(IEnumerable<object> input, object parameter)
        {
            return input == null || !input.Any() ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
