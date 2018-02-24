using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using JetBrains.Annotations;

namespace StarryEyes.Views.Adorners
{
    public abstract class AdornerBase : Adorner, IDisposable
    {
        protected AdornerLayer AdornerLayer { get; private set; }
        protected Grid Root { get; private set; }

        protected AdornerBase([CanBeNull] UIElement adornedElement)
            : base(adornedElement)
        {
            this.AdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            this.Root = new Grid();
            if (this.AdornerLayer != null)
            {
                this.AdornerLayer.Add(this);
            }
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Root.Measure(constraint);
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Root.Arrange(new Rect(finalSize));
            return base.ArrangeOverride(finalSize);
        }

        protected override System.Windows.Media.Visual GetVisualChild(int index)
        {
            if (index >= VisualChildrenCount)
            {
                throw new IndexOutOfRangeException("index " + index + ", item count is " + VisualChildrenCount);
            }
            return Root;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        ~AdornerBase()
        {
            this.Dispose(false);
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }
            this._disposed = true;
            AdornerLayer.Remove(this);
        }
    }
}
