using System.Windows;

namespace StarryEyes.Views.Utils
{
    public class BooleanToVisibleConverter : TwoWayConverter<bool, Visibility>
    {
        protected override Visibility ToTarget(bool input, object parameter)
        {
            return input ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override bool ToSource(Visibility input, object parameter)
        {
            return input == Visibility.Visible;
        }
    }

    public class BooleanToInvisibleConverter : TwoWayConverter<bool, Visibility>
    {
        protected override Visibility ToTarget(bool input, object parameter)
        {
            return input ? Visibility.Collapsed : Visibility.Visible;
        }

        protected override bool ToSource(Visibility input, object parameter)
        {
            return input == Visibility.Collapsed;
        }
    }

    public class BooleanInverterConverter : TwoWayConverter<bool, bool>
    {
        protected override bool ToTarget(bool input, object parameter)
        {
            return !input;
        }

        protected override bool ToSource(bool input, object parameter)
        {
            return !input;
        }
    }
}
