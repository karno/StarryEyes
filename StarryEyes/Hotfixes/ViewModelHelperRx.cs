using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Threading;
using Livet;
using Livet.EventListeners;
using StarryEyes.Hotfixes;

// ReSharper disable CheckNamespace
namespace StarryEyes
// ReSharper restore CheckNamespace
{
    /// <summary>
    /// ViewModelHelper based on Reactive Extensions.
    /// </summary>
    public static class ViewModelHelperRx
    {
        public static ReadOnlyDispatcherCollectionRx<TViewModel> CreateReadOnlyDispatcherCollectionRx<TModel, TViewModel>
            (ObservableSynchronizedCollectionEx<TModel> source, Func<TModel, TViewModel> converter, Dispatcher dispatcher, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (source == null) throw new ArgumentNullException("source");

            if (!DispatcherHelper.UIDispatcher.CheckAccess())
            {
                throw new ArgumentException("This method must be called on the Dispatcher thread.");
            }

            var sourceAsNotifyCollection = source as INotifyCollectionChanged;
            if (sourceAsNotifyCollection == null) throw new ArgumentException("sourceがINotifyCollectionChangedを実装していません");

            var initCollection = new ObservableCollection<TViewModel>();
            var target = new DispatcherCollectionRx<TViewModel>(initCollection, dispatcher)
            {
                CollectionChangedDispatcherPriority = priority
            };
            var result = new ReadOnlyDispatcherCollectionRx<TViewModel>(target);

            source.SynchronizedToArray(array =>
            {
                foreach (var model in array)
                {
                    initCollection.Add(converter(model));
                }
                result.Disposables.Add(CreateSubscription(sourceAsNotifyCollection, converter, target));
            });
            return result;
        }

        private static IDisposable CreateSubscription<TModel, TViewModel>(
            INotifyCollectionChanged source, Func<TModel, TViewModel> converter,
            DispatcherCollectionRx<TViewModel> target)
        {
            return new CollectionChangedEventListener(source, (o, e) =>
                DispatcherHelper.UIDispatcher.InvokeAsync(() =>
                {
                    if (e.NewItems != null && e.NewItems.Count >= 2)
                    {
                        throw new ArgumentException("Too many new items.");
                    }
                    try
                    {
                        switch (e.Action)
                        {
                            case NotifyCollectionChangedAction.Add:
                                if (e.NewItems == null)
                                {
                                    throw new ArgumentException("New item is null.");
                                }
                                target.Insert(e.NewStartingIndex, converter((TModel)e.NewItems[0]));
                                break;
                            case NotifyCollectionChangedAction.Move:
                                target.Move(e.OldStartingIndex, e.NewStartingIndex);
                                break;
                            case NotifyCollectionChangedAction.Remove:
                                if (typeof(IDisposable).IsAssignableFrom(typeof(TViewModel)))
                                {
                                    ((IDisposable)target[e.OldStartingIndex]).Dispose();
                                }
                                target.RemoveAt(e.OldStartingIndex);
                                break;
                            case NotifyCollectionChangedAction.Replace:
                                if (typeof(IDisposable).IsAssignableFrom(typeof(TViewModel)))
                                {
                                    ((IDisposable)target[e.NewStartingIndex]).Dispose();
                                }
                                if (e.NewItems == null)
                                {
                                    throw new ArgumentException("New item is null.");
                                }
                                target[e.NewStartingIndex] = converter((TModel)e.NewItems[0]);
                                break;
                            case NotifyCollectionChangedAction.Reset:
                                if (typeof(IDisposable).IsAssignableFrom(typeof(TViewModel)))
                                {
                                    // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                                    foreach (IDisposable item in target)
                                    {
                                        item.Dispose();
                                    }
                                }
                                target.Clear();
                                break;
                            default:
                                throw new ArgumentException();
                        }
                    }
                    catch (ArgumentOutOfRangeException aoex)
                    {
                        // collection inconsistent state
                        throw new InvalidOperationException(
                            "Collection state is invalid." + Environment.NewLine +
                            "INDEX OUT OF RANGE - " + e.Action + "[" + typeof(TModel).Name + " -> " +
                            typeof(TViewModel).Name + "]" + Environment.NewLine +
                            "new start: " + e.NewStartingIndex + ", count: " +
                            (e.NewItems == null
                                ? "null"
                                : e.NewItems.Count.ToString(CultureInfo.InvariantCulture)) +
                            Environment.NewLine +
                            "source length: " + ((IList<TModel>)source).Count + ", target length: " + target.Count +
                            ".",
                            aoex);
                    }
                }));
        }
    }
}
