using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace StarryEyes.Views.Behaviors
{
    public class TimelineScrollLockerBehavior : Behavior<ScrollViewer>
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public bool IsScrollLockEnabled
        {
            get { return (bool)GetValue(IsScrollLockEnabledProperty); }
            set { SetValue(IsScrollLockEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsScrollLockEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsScrollLockEnabledProperty =
            DependencyProperty.Register("IsScrollLockEnabled", typeof(bool),
            typeof(TimelineScrollLockerBehavior), new PropertyMetadata(false));

        protected override void OnAttached()
        {
            base.OnAttached();
            _disposables.Add(Observable.FromEventPattern<ScrollChangedEventHandler, ScrollChangedEventArgs>(
                h => this.AssociatedObject.ScrollChanged += h,
                h => this.AssociatedObject.ScrollChanged -= h)
                .Subscribe(_ => UpdateScrollHeight(this.AssociatedObject.ScrollableHeight)));
        }

        private double _previousHeight;
        private void UpdateScrollHeight(double value)
        {
            double p = value - _previousHeight;
            _previousHeight = value;
            if (p > 0 && IsScrollLockEnabled)
            {
                this.AssociatedObject.ScrollToVerticalOffset(
                    this.AssociatedObject.VerticalOffset + p);
            }
        }

        protected override void OnDetaching()
        {
            _disposables.Dispose();
        }

    }
}