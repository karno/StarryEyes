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
            (IList<TModel> source, Func<TModel, TViewModel> converter, Dispatcher dispatcher)
        {
            if (source == null) throw new ArgumentNullException("source");

            var sourceAsNotifyCollection = source as INotifyCollectionChanged;
            if (sourceAsNotifyCollection == null) throw new ArgumentException("sourceがINotifyCollectionChangedを実装していません");

            var initCollection = new ObservableCollection<TViewModel>();
            var internalLock = new object();
            lock (internalLock)
            {
                var target = new DispatcherCollectionRx<TViewModel>(initCollection, dispatcher);
                var result = new ReadOnlyDispatcherCollectionRx<TViewModel>(target);

                var subscribe = sourceAsNotifyCollection.ListenCollectionChanged()
                    .Subscribe(e =>
                    {
                        lock (internalLock)
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
                    });

                result.Disposables.Add(subscribe);

                foreach (var model in source)
                {
                    initCollection.Add(converter(model));
                }
                return result;
            }
        }
    }
}
