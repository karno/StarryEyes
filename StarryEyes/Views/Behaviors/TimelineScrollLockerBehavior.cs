using System.Collections;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace StarryEyes.Views.Behaviors
{
    public class TimelineScrollLockerBehavior : Behavior<ScrollViewer>
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private bool _isScrollLockEnabled;
        public bool IsScrollLockEnabled
        {
            get { return (bool)GetValue(IsScrollLockEnabledProperty); }
            set
            {
                SetValue(IsScrollLockEnabledProperty, value);
                _isScrollLockEnabled = value;
            }
        }

        // Using a DependencyProperty as the backing store for IsScrollLockEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsScrollLockEnabledProperty =
            DependencyProperty.Register("IsScrollLockEnabled", typeof(bool),
            typeof(TimelineScrollLockerBehavior), new PropertyMetadata(false, (sender, e) =>
            {
                ((TimelineScrollLockerBehavior)sender)._isScrollLockEnabled = (bool)e.NewValue;
            }));

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
                                 (sender, e) =>
                                 {
                                     if (e.ExtentHeightChange > 0 && IsScrollLockEnabled)
                                     {
                                         this.AssociatedObject.ScrollToVerticalOffset(e.VerticalOffset + e.ExtentHeightChange);
                                     }
                                 }));
        }

        protected override void OnDetaching()
        {
            _disposables.Dispose();
        }

    }
}