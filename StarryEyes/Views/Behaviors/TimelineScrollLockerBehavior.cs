using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace StarryEyes.Views.Behaviors
{
    public class TimelineScrollLockerBehavior : Behavior<ScrollViewer>
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public bool IsScrollLockEnabled
        {
            get { return (bool)GetValue(IsScrollLockEnabledProperty); }
            set
            {
                SetValue(IsScrollLockEnabledProperty, value);
            }
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
            DependencyProperty.Register("ItemsSource", typeof(IList), typeof(TimelineScrollLockerBehavior),
                                        new PropertyMetadata(null, ItemsSourceChanged));

        private int _previousItemCount;
        private double _previousScrollIndex;
        private IDisposable _itemSourceCollectionChangeListener;

        protected override void OnAttached()
        {
            base.OnAttached();
            _disposables.Add(Disposable.Create(() =>
            {
                var disposable = Interlocked.Exchange(ref _itemSourceCollectionChangeListener, null);
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }));

            _disposables.Add(
                Observable.FromEventPattern<ScrollChangedEventHandler, ScrollChangedEventArgs>(
                    h => this.AssociatedObject.ScrollChanged += h,
                    h => this.AssociatedObject.ScrollChanged -= h)
                          .Select(p => p.EventArgs)
                          .Subscribe(
                              e =>
                              {
                                  _previousScrollIndex = this.AssociatedObject.VerticalOffset;
                                  var source = ItemsSource;
                                  if (source == null) return;
                                  var itemCount = source.Count;
                                  if (_previousItemCount == itemCount) return;
                                  _previousItemCount = itemCount;
                                  if (e.ExtentHeightChange > 0)
                                  {
                                      this.AssociatedObject.ScrollToVerticalOffset(
                                          e.VerticalOffset + e.ExtentHeightChange);
                                      if (!IsScrollLockEnabled)
                                      {
                                          RunAnimation(e.VerticalOffset + e.ExtentHeightChange, e.ExtentHeightChange);
                                      }
                                  }
                              }));
        }

        private static void ItemsSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var behavior = dependencyObject as TimelineScrollLockerBehavior;
            if (behavior != null)
            {
                behavior.ItemsSourceChanged();
            }
        }

        private void ItemsSourceChanged()
        {
            _previousItemCount = ItemsSource.Count;
            var nc = ItemsSource as INotifyCollectionChanged;
            if (nc != null)
            {
                var listener = nc.ListenCollectionChanged()
                              .Subscribe(ev =>
                              {
                                  if (ev.Action == NotifyCollectionChangedAction.Add)
                                  {
                                      var vsp = FindVisualChild<VirtualizingStackPanel>(this.AssociatedObject);
                                      if (vsp != null)
                                      {
                                          var index = vsp.ItemContainerGenerator.IndexFromGeneratorPosition(
                                              new GeneratorPosition(0, 0));
                                          if (ev.NewStartingIndex > index)
                                          {
                                              _previousItemCount = ItemsSource.Count;
                                          }
                                      }
                                  }
                                  else
                                  {
                                      _previousItemCount = ItemsSource.Count;
                                  }
                              });
                var disposable = Interlocked.Exchange(
                    ref _itemSourceCollectionChangeListener, listener);
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }

        private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child is T)
                {
                    return (T)child;
                }
                else
                {
                    child = FindVisualChild<T>(child);
                    if (child != null)
                    {
                        return (T)child;
                    }
                }
            }
            return null;
        }

        private double _currentOffset;
        private double _remainHeight;
        private volatile bool _isAnimationRunning;
        private void RunAnimation(double offset, double height)
        {
            if (_remainHeight < 0) _remainHeight = 0;
            if (_currentOffset < 0) _currentOffset = 0;
            _currentOffset = offset;
            _remainHeight += height;
            if (_isAnimationRunning) return;
            _isAnimationRunning = true;
            Task.Run(() =>
            {
                for (int i = 20; i > 0; i--)
                {
                    Thread.Sleep(10);
                    var dx = _remainHeight / i;
                    _remainHeight -= dx;
                    _currentOffset -= dx;
                    DispatcherHolder.Enqueue(() => this.AssociatedObject.ScrollToVerticalOffset(_currentOffset));
                }
                _isAnimationRunning = false;
            });
        }

        protected override void OnDetaching()
        {
            _disposables.Dispose();
        }

    }
}