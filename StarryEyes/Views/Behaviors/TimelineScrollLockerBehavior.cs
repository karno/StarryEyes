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
        private CompositeDisposable _disposables = new CompositeDisposable();

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
                handler => this.AssociatedObject.ScrollChanged += handler,
                handler => this.AssociatedObject.ScrollChanged -= handler)
                .Subscribe(_ => UpdateScrollHeight(this.AssociatedObject.ScrollableHeight)));
        }

        private double _previousHeight;
        private void UpdateScrollHeight(double value)
        {
            if (value > _previousHeight && IsScrollLockEnabled)
            {
                SetScroll(value - _previousHeight);
            }
            _previousHeight = value;
        }

        protected override void OnDetaching()
        {
            _disposables.Dispose();
        }

        private object _syncLock = new object();
        private double _scrollWaitCount = 0;
        private void SetScroll(double distance)
        {
            double _remain = 0;
            lock (_syncLock)
            {
                _scrollWaitCount += distance;
                _remain = _scrollWaitCount;
            }
            if (_remain == distance)
                SetScrollSynchronized();
        }

        private void SetScrollSynchronized()
        {
            double value;
            lock (_syncLock)
            {
                value = _scrollWaitCount;
                _scrollWaitCount = 0;
            }
            this.AssociatedObject.ScrollToVerticalOffset(
                this.AssociatedObject.VerticalOffset + value);
        }
    }
}
