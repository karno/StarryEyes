using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace StarryEyes.Views.Behaviors
{
    public class TimelineTriggerBehavior : Behavior<ScrollViewer>
    {
        private CompositeDisposable _disposables = new CompositeDisposable();

        public double ScrollOffset
        {
            get { return (int)GetValue(ScrollOffsetProperty); }
            set { SetValue(ScrollOffsetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScrollOffset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScrollOffsetProperty =
            DependencyProperty.Register("ScrollOffset", typeof(double), typeof(TimelineTriggerBehavior), new PropertyMetadata(0.0));

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
                Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                handler => this.AssociatedObject.MouseEnter += handler,
                handler => this.AssociatedObject.MouseEnter -= handler)
                .Subscribe(_ => this.IsMouseOver = this.AssociatedObject.IsMouseOver));
            this._disposables.Add(
                Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                handler => this.AssociatedObject.MouseLeave += handler,
                handler => this.AssociatedObject.MouseLeave -= handler)
                .Subscribe(_ => this.IsMouseOver = this.AssociatedObject.IsMouseOver));
            this._disposables.Add(
                Observable.FromEventPattern<ScrollChangedEventHandler, ScrollChangedEventArgs>(
                handler => this.AssociatedObject.ScrollChanged += handler,
                handler => this.AssociatedObject.ScrollChanged -= handler)
                .Subscribe(_ => this.ScrollOffset = this.AssociatedObject.VerticalOffset));
        }

        protected override void OnDetaching()
        {
            this._disposables.Dispose();
            base.OnDetaching();
        }

    }
}
