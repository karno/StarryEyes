using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows.Threading;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Hotfixes;

// ReSharper disable once CheckNamespace
namespace StarryEyes
{
    public class ReadOnlyDispatcherCollectionRx<T> : ReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
    {
        private readonly DispatcherCollectionRx<T> _collection;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private bool _disposed;

        public ReadOnlyDispatcherCollectionRx(DispatcherCollectionRx<T> collection)
            : base(collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            _collection = collection;

            _disposables.Add(_collection.ListenPropertyChanged(OnPropertyChanged));
            _disposables.Add(_collection.ListenCollectionChanged(OnCollectionChanged));
        }

        /// <summary>
        /// このコレクションが変更通知を行うDispatcherを取得します。
        /// </summary>
        public Dispatcher Dispatcher
        {
            get
            {
                ThrowExceptionIfDisposed();
                return _collection.Dispatcher;
            }
        }

        /// <summary>
        /// この読み取り専用コレクションのソースDispatcherCollectionを取得します。
        /// </summary>
        public DispatcherCollectionRx<T> SourceCollection
        {
            get
            {
                ThrowExceptionIfDisposed();
                return _collection;
            }
        }

        /// <summary>
        /// この読み取り専用コレクションが保持するイベントリスナのコレクションを取得します。
        /// </summary>
        public CompositeDisposable Disposables
        {
            get
            {
                ThrowExceptionIfDisposed();
                return _disposables;
            }
        }

        /// <summary>
        /// コレクションが変更された時に発生します。
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// プロパティが変更された時に発生します。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            ThrowExceptionIfDisposed();
            var threadSafeHandler = Interlocked.CompareExchange(ref CollectionChanged, null, null);

            if (threadSafeHandler != null)
            {
                threadSafeHandler(this, args);
            }
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            ThrowExceptionIfDisposed();
            var threadSafeHandler = Interlocked.CompareExchange(ref PropertyChanged, null, null);

            if (threadSafeHandler != null)
            {
                threadSafeHandler(this, args);
            }
        }

        /// <summary>
        /// ソースコレクションとの連動を解除します。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _disposables.Dispose();

                if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
                {
                    // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                    foreach (IDisposable i in _collection)
                    {
                        i.Dispose();
                    }
                }

                _collection.Dispose();
            }
            _disposed = true;
        }

        protected void ThrowExceptionIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("ReadOnlyDispatcherCollection");
            }
        }
    }
}
