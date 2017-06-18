using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using Sophia.Utilities;

namespace Sophia.Collections
{
    public class SynchronizedObservableCollection<T> : ObservableCollection<T>
    {
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public SynchronizedObservableCollection()
        {
        }

        public SynchronizedObservableCollection(IEnumerable<T> initialItems)
        {
            foreach (var item in initialItems)
            {
                Add(item);
            }
        }


        protected override void ClearItems()
        {
            // Check reentrancy before acquiring WriteLock.
            CheckReentrancy();
            using (AcquireWriteLock())
            {
                base.ClearItems();
            }
        }

        protected override void InsertItem(int index, T item)
        {
            CheckReentrancy();
            using (AcquireWriteLock())
            {
                base.InsertItem(index, item);
            }
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            CheckReentrancy();
            using (AcquireWriteLock())
            {
                base.MoveItem(oldIndex, newIndex);
            }
        }

        protected override void RemoveItem(int index)
        {
            CheckReentrancy();
            using (AcquireWriteLock())
            {
                base.RemoveItem(index);
            }
        }

        protected override void SetItem(int index, T item)
        {
            CheckReentrancy();
            using (AcquireWriteLock())
            {
                base.SetItem(index, item);
            }
        }

        public virtual IDisposable AcquireReadLock()
        {
            _lockSlim.EnterReadLock();
            return Disposable.Create(() => _lockSlim.ExitReadLock());
        }

        protected virtual IDisposable AcquireWriteLock()
        {
            _lockSlim.EnterWriteLock();
            return Disposable.Create(() => _lockSlim.ExitWriteLock());
        }


        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            _lockSlim.EnterReadLock();
            try
            {
                base.OnCollectionChanged(e);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }
    }
}