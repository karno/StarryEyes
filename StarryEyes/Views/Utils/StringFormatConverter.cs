using System;

namespace StarryEyes.Views.Utils
{
    public class StringFormatConverter : OneWayConverter<object, string>
    {
        protected override string ToTarget(object input, object parameter)
        {
            return String.Format(parameter as string ?? "{0}", input);
        }
    }
}
