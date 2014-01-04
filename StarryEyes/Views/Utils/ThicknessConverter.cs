using System.Windows;

namespace StarryEyes.Views.Utils
{
    public class BottomInvertedThicknessConverter : OneWayConverter<double, Thickness>
    {
        protected override Thickness ToTarget(double input, object parameter)
        {
            return new Thickness(0, 0, 0, -input);
        }
    }
}
