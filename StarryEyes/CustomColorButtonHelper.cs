using System.Windows;
using System.Windows.Media;

namespace StarryEyes
{
    public static class CustomColorButtonHelper
    {
        public static readonly DependencyProperty DefaultBrushProperty = DependencyProperty.RegisterAttached(
            "DefaultBrush", typeof(Brush), typeof(CustomColorButtonHelper), new PropertyMetadata(default(Brush)));

        public static void SetDefaultBrush(DependencyObject element, Brush value)
        {
            element.SetValue(DefaultBrushProperty, value);
        }

        public static Brush GetDefaultBrush(DependencyObject element)
        {
            return (Brush)element.GetValue(DefaultBrushProperty);
        }

        public static readonly DependencyProperty HoverBrushProperty = DependencyProperty.RegisterAttached(
            "HoverBrush", typeof(Brush), typeof(CustomColorButtonHelper), new PropertyMetadata(default(Brush)));

        public static void SetHoverBrush(DependencyObject element, Brush value)
        {
            element.SetValue(HoverBrushProperty, value);
        }

        public static Brush GetHoverBrush(DependencyObject element)
        {
            return (Brush)element.GetValue(HoverBrushProperty);
        }

        public static readonly DependencyProperty PressedBrushProperty = DependencyProperty.RegisterAttached(
            "PressedBrush", typeof(Brush), typeof(CustomColorButtonHelper), new PropertyMetadata(default(Brush)));

        public static void SetPressedBrush(DependencyObject element, Brush value)
        {
            element.SetValue(PressedBrushProperty, value);
        }

        public static Brush GetPressedBrush(DependencyObject element)
        {
            return (Brush)element.GetValue(PressedBrushProperty);
        }

        public static readonly DependencyProperty DefaultForegroundBrushProperty = DependencyProperty.RegisterAttached(
            "DefaultForegroundBrush", typeof(Brush), typeof(CustomColorButtonHelper), new PropertyMetadata(Brushes.Yellow));

        public static void SetDefaultForegroundBrush(DependencyObject element, Brush value)
        {
            element.SetValue(DefaultForegroundBrushProperty, value);
        }

        public static Brush GetDefaultForegroundBrush(DependencyObject element)
        {
            return (Brush)element.GetValue(DefaultForegroundBrushProperty);
        }

        public static readonly DependencyProperty HoverForegroundBrushProperty = DependencyProperty.RegisterAttached(
            "HoverForegroundBrush", typeof(Brush), typeof(CustomColorButtonHelper), new PropertyMetadata(default(Brush)));

        public static void SetHoverForegroundBrush(DependencyObject element, Brush value)
        {
            element.SetValue(HoverForegroundBrushProperty, value);
        }

        public static Brush GetHoverForegroundBrush(DependencyObject element)
        {
            return (Brush)element.GetValue(HoverForegroundBrushProperty);
        }

        public static readonly DependencyProperty PressedForegroundBrushProperty = DependencyProperty.RegisterAttached(
            "PressedForegroundBrush", typeof(Brush), typeof(CustomColorButtonHelper), new PropertyMetadata(default(Brush)));

        public static void SetPressedForegroundBrush(DependencyObject element, Brush value)
        {
            element.SetValue(PressedForegroundBrushProperty, value);
        }

        public static Brush GetPressedForegroundBrush(DependencyObject element)
        {
            return (Brush)element.GetValue(PressedForegroundBrushProperty);
        }

    }
}
