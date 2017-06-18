using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using JetBrains.Annotations;

namespace Sophia.Windows
{
    public abstract class InsertionAdornerBase : Adorner, IDisposable
    {
        protected InsertionAdornerBase([NotNull] UIElement adornedElement) : base(adornedElement)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~InsertionAdornerBase()
        {
            Dispose(false);
        }

        protected abstract void Dispose(bool disposing);
    }

    public class InsertionAdorner : InsertionAdornerBase
    {
        private readonly AdornerLayer _adornerLayer;
        private readonly UIElement _content;

        protected InsertionAdorner([NotNull] UIElement adornedElement, UIElement content)
        : base(adornedElement)
        {
            _content = content;
            _adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            _adornerLayer?.Add(this);
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _content.Measure(constraint);
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _content.Arrange(new Rect(finalSize));
            return base.ArrangeOverride(finalSize);
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index >= VisualChildrenCount)
            {
                throw new IndexOutOfRangeException("index " + index + ", item count is " + VisualChildrenCount);
            }
            return _content;
        }

        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _adornerLayer.Remove(this);
        }
    }
}