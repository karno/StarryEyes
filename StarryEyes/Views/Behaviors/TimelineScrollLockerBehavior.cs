using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using System.Windows.Threading;
using StarryEyes.Views.Utils;

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
                                  // get source
                                  var source = ItemsSource;
                                  if (source == null) return;

                                  // check and update items count latch
                                  var itemCount = source.Count;
                                  if (_previousItemCount == itemCount) return;
                                  _previousItemCount = itemCount;

                                  // if scroll extent is not changed or shrinked, nothing to do.
                                  if (e.ExtentHeightChange <= 0) return;

                                  // calculate position should scroll to.
                                  var prevPosition = e.VerticalOffset + e.ExtentHeightChange;

                                  // scroll back to previous position
                                  if (this.IsScrollLockEnabled && !_isAnimationRunning)
                                  {
                                      this.AssociatedObject.ScrollToVerticalOffset(prevPosition);
                                  }
                                  else
                                  {
                                      // animate to new position
                                      this.RunAnimation(prevPosition, e.ExtentHeightChange);
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

        /// <summary>
        /// Reflect changing items source
        /// </summary>
        private void ItemsSourceChanged()
        {
            // capture current value
            var itemsSource = ItemsSource;

            // validate interface
            var nc = itemsSource as INotifyCollectionChanged;
            if (nc == null) return;

            // initialize count
            this._previousItemCount = itemsSource.Count;

            // create listener of timeline changes
            var listener =
                nc.ListenCollectionChanged()
                  .Subscribe(ev =>
                  {
                      if (ev.Action == NotifyCollectionChangedAction.Add)
                      {
                          // get virtualizing stack panel
                          var vsp = this.AssociatedObject.FindVisualChild<VirtualizingStackPanel>();
                          if (vsp != null)
                          {
                              var index = vsp.ItemContainerGenerator
                                             .IndexFromGeneratorPosition(new GeneratorPosition(0, 0));
                              // check new index is newer than the bottom item of current viewport items.
                              if (ev.NewStartingIndex <= index)
                              {
                                  return;
                              }
                          }
                      }
                      // we should not scroll -> update items count.
                      this._previousItemCount = itemsSource.Count;
                  });

            // swap old listener
            var disposable = Interlocked.Exchange(
                ref this._itemSourceCollectionChangeListener, listener);
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        private double _remainHeight;
        private readonly object _animationLock = new object();
        private bool _isAnimationRunning;

        /// <summary>
        /// Run scroll animation.
        /// </summary>
        /// <param name="beginOffset">beginning scroll beginOffset</param>
        /// <param name="extent">animation extent(to scroll)</param>
        private void RunAnimation(double beginOffset, double extent)
        {
            lock (_animationLock)
            {
                if (_isAnimationRunning)
                {
                    _remainHeight += extent;
                    return;
                }
            }

            _isAnimationRunning = true;
            _remainHeight = extent;

            this.AssociatedObject.ScrollToVerticalOffset(beginOffset);

            var timer = new DispatcherTimer(DispatcherPriority.Render, DispatcherHolder.Dispatcher);
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += (o, e) =>
            {
                var d = _remainHeight / 20;
                if (d < 3)
                {
                    d = 3;
                }
                _remainHeight -= d;
                var newpos = this.AssociatedObject.VerticalOffset - d;
                this.AssociatedObject.ScrollToVerticalOffset(newpos > 1 ? newpos : 0);
                lock (_animationLock)
                {
                    if (_remainHeight > 0)
                    {
                        return;
                    }
                    // stop animation
                    _isAnimationRunning = false;
                }
                timer.Stop();
                timer = null;
            };
            timer.Start();
        }

        protected override void OnDetaching()
        {
            _disposables.Dispose();
        }

    }
}