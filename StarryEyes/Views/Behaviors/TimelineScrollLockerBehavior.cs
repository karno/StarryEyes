using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Threading;

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

        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IList), typeof(TimelineScrollLockerBehavior), new PropertyMetadata(null));

        protected override void OnAttached()
        {
            base.OnAttached();
            _disposables.Add(new Livet.EventListeners.EventListener<ScrollChangedEventHandler>(
                                 h => this.AssociatedObject.ScrollChanged += h,
                                 h => this.AssociatedObject.ScrollChanged -= h,
                                 (sender, e) => UpdateScrollHeight(this.AssociatedObject.ScrollableHeight)));
        }

        private double _previousHeight;
        private int _itemsCount;
        private void UpdateScrollHeight(double value)
        {
            if (ItemsSource != null)
            {
                var pc = _itemsCount;
                _itemsCount = ItemsSource.Count;
                if (pc == _itemsCount) return; // item count is not changed.
            }
            double p = value - _previousHeight;
            _previousHeight = value;
            if (p > 0 && IsScrollLockEnabled)
            {
                this.AssociatedObject.ScrollToVerticalOffset(this.AssociatedObject.VerticalOffset + p);
            }
        }

        protected override void OnDetaching()
        {
            _disposables.Dispose();
        }

    }
}