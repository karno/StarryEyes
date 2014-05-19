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
                                  this.AssociatedObject.ScrollToVerticalOffset(prevPosition);
                                  System.Diagnostics.Debug.WriteLine("*** SCROLL (LOCK) ***");

                                  if (!this.IsScrollLockEnabled)
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

        private double _currentOffset;
        private double _remainHeight;
        private volatile bool _isAnimationRunning;
        /// <summary>
        /// Run scroll animation.
        /// </summary>
        /// <param name="offset">beginning scroll offset</param>
        /// <param name="height">animation height(to scroll)</param>
        private void RunAnimation(double offset, double height)
        {
            // reset remain height
            if (_remainHeight < 0)
            {
                _remainHeight = 0;
            }
            // scroll start offset
            _currentOffset = offset;
            // scroll height
            _remainHeight += height;

            if (_isAnimationRunning) return;
            _isAnimationRunning = true;
            Task.Run(() =>
            {
                for (var i = 20; i > 0; i--)
                {
                    Thread.Sleep(10);
                    var dx = _remainHeight / i;
                    _remainHeight -= dx;
                    _currentOffset -= dx;
                    DispatcherHolder.Enqueue(() => this.AssociatedObject.ScrollToVerticalOffset(_currentOffset));
                    System.Diagnostics.Debug.WriteLine("*** SCROLL (ANIMATE) ***");
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