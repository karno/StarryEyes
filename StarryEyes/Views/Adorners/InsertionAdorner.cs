using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
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
            return new Rectangle
            {
                Margin = new Thickness(-1.5, 0, 0, 0),
                Width = 3,
                Fill = Brushes.Black,
                Opacity = 0.7,
            };
        }
    }
}
