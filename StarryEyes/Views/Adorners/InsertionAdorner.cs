using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Expression.Shapes;
using StarryEyes.Annotations;

namespace StarryEyes.Views.Adorners
{
    public class InsertionAdorner : AdornerBase
    {
        public InsertionAdorner([NotNull] UIElement adornedElement, bool lastItem = false)
            : base(adornedElement)
        {
            var ctrl = CreateCursor();
            Root.Children.Add(ctrl);
            ctrl.SetValue(HorizontalAlignmentProperty, lastItem ? HorizontalAlignment.Right : HorizontalAlignment.Left);
            ctrl.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
        }

        private UIElement CreateCursor()
        {
            return new RegularPolygon
            {
                Margin = new Thickness(-8, -10, 0, 0),
                Fill = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Height = 8,
                Width = 8,
                PointCount = 3,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1, -1),
            };
        }
    }
}
