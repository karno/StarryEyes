using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Threading;
using JetBrains.Annotations;

namespace Sophia.Collections
{
    public static class DispatcherObservableCollectionWrapper
    {
        public static DispatcherObservableCollectionWrapper<TViewModel, TItem> Wrap<TViewModel, TItem>(
            [NotNull] ObservableCollection<TItem> collection, [NotNull] Func<TItem, TViewModel> converter,
            [NotNull] Dispatcher bindingDispatcher, DispatcherPriority priority)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (bindingDispatcher == null) throw new ArgumentNullException(nameof(bindingDispatcher));
            return new DispatcherObservableCollectionWrapper<TViewModel, TItem>(collection, converter, bindingDispatcher,
                priority);
        }
    }

    public class DispatcherObservableCollectionWrapper<TViewModel, TItem> : ReadOnlyCollection<TViewModel>,
        INotifyCollectionChanged, IDisposable
    {
        private readonly Dispatcher _bindingDispatcher;
        private readonly DispatcherPriority _priority;
        private readonly ObservableCollection<TItem> _bindingCollection;
        private readonly Func<TItem, TViewModel> _converter;

        private bool _disposed;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public DispatcherObservableCollectionWrapper([NotNull] ObservableCollection<TItem> collection,
            [NotNull] Func<TItem, TViewModel> converter, [NotNull] Dispatcher bindingDispatcher,
            DispatcherPriority priority) : base(new List<TViewModel>())
        {
            if (bindingDispatcher == null) throw new ArgumentNullException(nameof(bindingDispatcher));
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            _bindingDispatcher = bindingDispatcher;
            _priority = priority;
            _bindingCollection = collection;
            _converter = converter;
            SubscribeNotification();

            // initialize collection
            var synchronizedCollection = collection as SynchronizedObservableCollection<TItem>;
            var readLock = synchronizedCollection?.AcquireReadLock();
            foreach (var item in _bindingCollection)
            {
                Items.Add(converter(item));
            }
            readLock?.Dispose();
        }

        private void SubscribeNotification()
        {
            _bindingCollection.CollectionChanged += BindingCollectionChanged;
        }

        protected void UnsubscribeNotification()
        {
            _bindingCollection.CollectionChanged -= BindingCollectionChanged;
        }

        protected virtual void BindingCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_disposed) return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddItem(e.NewStartingIndex, (TItem)e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveItem(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    ReplaceItem(e.NewStartingIndex, (TItem)e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Move:
                    MoveItem(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Reset();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual void Reset()
        {
            _bindingDispatcher.InvokeAsync(() =>
            {
                var removals = Items.ToArray();
                Reset();
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
                foreach (var vm in removals)
                {
                    ReleaseConvertedItem(vm);
                }
            }, _priority);
        }

        protected virtual void MoveItem(int oldStartingIndex, int newStartingIndex)
        {
            _bindingDispatcher.InvokeAsync(() =>
            {
                var item = Items[oldStartingIndex];
                Items.RemoveAt(oldStartingIndex);
                Items.Insert(newStartingIndex, item);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Move,
                    item, newStartingIndex, oldStartingIndex));
            }, _priority);
        }

        protected virtual void ReplaceItem(int newStartingIndex, TItem newItem)
        {
            _bindingDispatcher.InvokeAsync(() =>
            {
                var oldItem = Items[newStartingIndex];
                var convertedNewItem = _converter(newItem);
                Items[newStartingIndex] = convertedNewItem;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, convertedNewItem, oldItem, newStartingIndex));
                ReleaseConvertedItem(oldItem);
            }, _priority);
        }

        protected virtual void AddItem(int newStartingIndex, TItem newItem)
        {
            _bindingDispatcher.InvokeAsync(() =>
            {
                var convertedNewItem = _converter(newItem);
                Items.Insert(newStartingIndex, convertedNewItem);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, convertedNewItem, newStartingIndex));
            }, _priority);
        }

        protected virtual void RemoveItem(int oldStartingIndex)
        {
            _bindingDispatcher.InvokeAsync(() =>
            {
                var oldItem = Items[oldStartingIndex];
                Items.RemoveAt(oldStartingIndex);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, oldItem, oldStartingIndex));
                ReleaseConvertedItem(oldItem);
            }, _priority);
        }

        private void ReleaseConvertedItem(TViewModel item)
        {
            (item as IDisposable)?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DispatcherObservableCollectionWrapper()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                UnsubscribeNotification();
                var items = Items.ToArray();
                foreach (var vm in items)
                {
                    ReleaseConvertedItem(vm);
                }
            }
            _disposed = true;
        }
    }
}