using System;
using System.Globalization;
using System.Windows.Data;
using Sophia.Utilities;

namespace Sophia.Converters
{
    // Converter templates
    public abstract class TwoWayConverter<TSource, TTarget> : ConvertBase, IValueConverter
    {
        protected abstract TTarget ToTarget(TSource input, object parameter, CultureInfo culture);

        protected abstract TSource ToSource(TTarget input, object parameter, CultureInfo culture);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertSink<TSource, TTarget>(value, parameter, culture, ToTarget);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertSink<TTarget, TSource>(value, parameter, culture, ToSource);
        }
    }

    public abstract class OneWayConverter<TSource, TTarget> : ConvertBase, IValueConverter
    {
        protected abstract TTarget ToTarget(TSource input, object parameter, CultureInfo culture);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertSink<TSource, TTarget>(value, parameter, culture, ToTarget);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public abstract class ConvertBase
    {
        protected static TTarget ConvertSink<TSource, TTarget>(object value, object parameter, CultureInfo culture,
            Func<TSource, object, CultureInfo, TTarget> converter)
        {
            if (DesignTimeHelper.IsInDesignTime())
            {
                try
                {
                    return converter((TSource)value, parameter, culture);
                }
                catch
                {
                    return converter(default(TSource), parameter, culture);
                }
            }
            return converter((TSource)value, parameter, culture);
        }
    }
}