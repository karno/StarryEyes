using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using Livet.EventListeners;

namespace StarryEyes.Views.Behaviors
{
    public class TimelineTriggerBehavior : Behavior<ScrollViewer>
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public bool IsScrollOnTop
        {
            get { return (bool)GetValue(IsScrollOnTopProperty); }
            set { SetValue(IsScrollOnTopProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsScrollOnTop.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsScrollOnTopProperty =
            DependencyProperty.Register("IsScrollOnTop", typeof(bool), typeof(TimelineTriggerBehavior), new PropertyMetadata(false));

        public bool IsScrollOnBottom
        {
            get { return (bool)GetValue(IsScrollOnBottomProperty); }
            set { SetValue(IsScrollOnBottomProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsScrollOnBottom.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsScrollOnBottomProperty =
            DependencyProperty.Register("IsScrollOnBottom", typeof(bool), typeof(TimelineTriggerBehavior), new PropertyMetadata(false));

        public bool IsMouseOver
        {
            get { return (bool)GetValue(IsMouseOverProperty); }
            set { SetValue(IsMouseOverProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMouseOver.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMouseOverProperty =
            DependencyProperty.Register("IsMouseOver", typeof(bool), typeof(TimelineTriggerBehavior), new PropertyMetadata(false));

        protected override void OnAttached()
        {
            base.OnAttached();
            this._disposables.Add(
                new EventListener<MouseEventHandler>(
                    h => this.AssociatedObject.MouseEnter += h,
                    h => this.AssociatedObject.MouseEnter -= h,
                    (_, __) => this.IsMouseOver = this.AssociatedObject.IsMouseOver));
            this._disposables.Add(
                new EventListener<MouseEventHandler>(
                    h => this.AssociatedObject.MouseLeave += h,
                    h => this.AssociatedObject.MouseLeave -= h,
                    (_, __) => this.IsMouseOver = this.AssociatedObject.IsMouseOver));
            this._disposables.Add(
                new EventListener<ScrollChangedEventHandler>(
                    h => this.AssociatedObject.ScrollChanged += h,
                    h => this.AssociatedObject.ScrollChanged -= h,
                    (_, __) =>
                    {
                        bool top = this.AssociatedObject.VerticalOffset < 1;
                        bool bottom = this.AssociatedObject.VerticalOffset > this.AssociatedObject.ScrollableHeight - 1;
                        IsScrollOnTop = top;
                        IsScrollOnBottom = bottom;
                    }));
        }

        protected override void OnDetaching()
        {
            this._disposables.Dispose();
            base.OnDetaching();
        }

    }
}
