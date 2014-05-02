using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Windows.Threading;
using Livet;
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
            (IList<TModel> source, Func<TModel, TViewModel> converter, Dispatcher dispatcher, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (source == null) throw new ArgumentNullException("source");

            if (!DispatcherHolder.Dispatcher.CheckAccess())
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

            var scx = source as ObservableSynchronizedCollectionEx<TModel>;
            if (scx != null)
            {
                scx.SynchronizedToArray(array =>
                {
                    result.Disposables.Add(CreateSubscription(sourceAsNotifyCollection, converter, target));
                    foreach (var model in array)
                    {
                        initCollection.Add(converter(model));
                    }
                });
            }
            else
            {
                foreach (var model in source)
                {
                    initCollection.Add(converter(model));
                }
                result.Disposables.Add(CreateSubscription(sourceAsNotifyCollection, converter, target));
            }
            return result;
        }

        private static IDisposable CreateSubscription<TModel, TViewModel>(
            INotifyCollectionChanged source, Func<TModel, TViewModel> converter,
            DispatcherCollectionRx<TViewModel> target)
        {
            return source
                .ListenCollectionChanged()
                .ObserveOn(DispatcherHolder.Dispatcher)
                .Subscribe(e =>
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
                                target[e.NewStartingIndex] = converter((TModel)e.NewItems[0]);
                                break;
                            case NotifyCollectionChangedAction.Reset:
                                if (typeof(IDisposable).IsAssignableFrom(typeof(TViewModel)))
                                {
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
                        throw new InvalidOperationException(
                            "Collection state is invalid." + Environment.NewLine +
                            "INDEX OUT OF RANGE - " + e.Action + "[" + typeof(TModel).Name + " -> " +
                            typeof(TViewModel).Name + ", reset: " + " ]" + Environment.NewLine +
                            "new start: " + e.NewStartingIndex + ", count: " +
                            (e.NewItems == null ? "null" : e.NewItems.Count.ToString()) + Environment.NewLine +
                            "source length: " + ((IList<TModel>)source).Count + ", target length: " + target.Count +
                            ".",
                            aoex);
                    }
                });
        }
    }
}
