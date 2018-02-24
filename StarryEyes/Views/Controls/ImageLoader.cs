
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using JetBrains.Annotations;
using Livet;

namespace StarryEyes.Views.Controls
{
    public static class ImageLoader
    {
        private const int MaxRetryCount = 3;
        private const int MaxReceiveConcurrency = 2;

        private const int LoadTimeoutMillisec = 3000;

        private static readonly Timer _expirationTimer;

        // Image process parameters
        private const int MaxLoadQueueSize = 256;
        private const int MaxDecodeQueueSize = 512;
        private const int MaxApplyImagePerOnce = 16;
        private const ImageProcessStrategy LoadStrategy = ImageProcessStrategy.LifoStack;
        private const ImageProcessStrategy DecodeStrategy = ImageProcessStrategy.LifoStack;

        static ImageLoader()
        {
            _expirationTimer = new Timer(_ => RemoveExpiredCaches(), null,
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            App.ApplicationFinalize += () => _expirationTimer.Dispose();
            StartDecodeThread();
        }

        #region Special resolver table

        private static readonly ConcurrentDictionary<string, Func<Uri, byte[]>> _specialTable =
            new ConcurrentDictionary<string, Func<Uri, byte[]>>();

        public static bool RegisterSpecialResolverTable(string scheme, Func<Uri, byte[]> resolver)
        {
            return _specialTable.TryAdd(scheme, resolver);
        }

        #endregion

        #region Image cache

        private const int MaxCacheCount = 512;

        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

        private static readonly LinkedList<Tuple<Uri, byte[], DateTime>> _cacheList =
            new LinkedList<Tuple<Uri, byte[], DateTime>>();

        private static readonly Dictionary<Uri, LinkedListNode<Tuple<Uri, byte[], DateTime>>> _cacheTable =
            new Dictionary<Uri, LinkedListNode<Tuple<Uri, byte[], DateTime>>>();

        private static bool GetCache([CanBeNull] Uri uri, out byte[] cache)
        {
            cache = null;
            if (uri == null) throw new ArgumentNullException("uri");
            LinkedListNode<Tuple<Uri, byte[], DateTime>> data;
            lock (_cacheList)
            {
                if (!_cacheTable.TryGetValue(uri, out data))
                {
                    // cache not found
                    return false;
                }
                if (DateTime.Now - data.Value.Item3 > CacheExpiration)
                {
                    // cache is explired
                    _cacheList.Remove(data);
                    _cacheTable.Remove(uri);
                    return false;
                }
                // move to head
                _cacheList.Remove(data);
                _cacheList.AddFirst(data);
            }
            cache = data.Value.Item2;
            return true;
        }

        private static void SetCache([CanBeNull] Uri uri, [CanBeNull]byte[] imageByte)
        {
            lock (_cacheList)
            {
                // remove before adding data
                LinkedListNode<Tuple<Uri, byte[], DateTime>> data;
                if (_cacheTable.TryGetValue(uri, out data))
                {
                    // cache is already existed
                    _cacheTable.Remove(uri);
                    _cacheList.Remove(data);
                }
                var created = _cacheList.AddFirst(Tuple.Create(uri, imageByte, DateTime.Now));
                _cacheTable[uri] = created;

                // trim overflow cache
                while (_cacheList.Count > MaxCacheCount)
                {
                    _cacheTable.Remove(_cacheList.Last.Value.Item1);
                    _cacheList.RemoveLast();
                }
            }
        }

        private static void RemoveExpiredCaches()
        {
            Tuple<Uri, byte[], DateTime>[] list;
            lock (_cacheList)
            {
                // copy current cache
                list = _cacheList.ToArray();
            }
            var expireds = list.Where(node => DateTime.Now - node.Item3 > CacheExpiration)
                               .ToList();
            lock (_cacheList)
            {
                // remove expireds
                foreach (var tuple in expireds)
                {
                    _cacheTable.Remove(tuple.Item1);
                    _cacheList.Remove(tuple);
                }
            }
        }

        #endregion

        #region Image Loader

        private static int _loadThreadConcurrency;

        private static readonly Dictionary<Uri, HashSet<Guid>> _loadTable =
            new Dictionary<Uri, HashSet<Guid>>();

        private static readonly LinkedList<Uri> _loadStack =
            new LinkedList<Uri>();

        private static void QueueLoadTask(Uri source, Guid id)
        {
            // _loadTable lock flag
            var locked = false;

            try
            {
                // acquire lock of _loadTable.
                Monitor.Enter(_loadTable, ref locked);

                HashSet<Guid> set; // callback hashset
                var queueingRequired = false; // Uri queue flag

                // lookup callback hashset from loader table.
                if (!_loadTable.TryGetValue(source, out set))
                {
                    set = new HashSet<Guid>();
                    _loadTable[source] = set;
                    queueingRequired = true; // set is not existed.
                }

                // acquire lock of internal set.
                lock (set)
                {
                    // release lock of _loadTable.
                    Monitor.Exit(_loadTable);
                    locked = false;

                    // add current id to callback set.
                    set.Add(id);

                    if (!queueingRequired)
                    {
                        // load action is already queued about this Uri. 
                        return;
                    }
                }
            }
            finally
            {
                // ensure lock of _loadTable is released.
                if (locked)
                {
                    Monitor.Exit(_loadTable);
                }
            }

            // removal item
            Uri removal = null;
            lock (_loadStack)
            {
#pragma warning disable 162
                // ReSharper disable HeuristicUnreachableCode

                // Leak lowest priority item of current queue.
                switch (LoadStrategy)
                {
                    case ImageProcessStrategy.FifoQueue:
                        _loadStack.AddLast(source);
                        if (_loadStack.Count > MaxLoadQueueSize)
                        {
                            // check overflow
                            removal = _loadStack.First.Value;
                            _loadStack.RemoveFirst();
                        }
                        break;
                    case ImageProcessStrategy.LifoStack:
                        _loadStack.AddFirst(source);
                        if (_loadStack.Count > MaxLoadQueueSize)
                        {
                            // check overflow
                            removal = _loadStack.Last.Value;
                            _loadStack.RemoveLast();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                // ReSharper restore HeuristicUnreachableCode
#pragma warning restore 162
            }
            if (removal != null)
            {
                CleanInternalTables(removal);
            }

            // check concurrency
            if (Interlocked.Increment(ref _loadThreadConcurrency) > MaxReceiveConcurrency)
            {
                Interlocked.Decrement(ref _loadThreadConcurrency);
                return;
            }
            // run task
            RunNextLoadTask();
        }

        private static void RunNextLoadTask()
        {
            Uri source;
            lock (_loadStack)
            {
                if (_loadStack.Count == 0)
                {
                    Interlocked.Decrement(ref _loadThreadConcurrency);
                    return;
                }
                source = _loadStack.First.Value;
                _loadStack.RemoveFirst();
            }
            Task.Run(async () => await LoadBytes(source).ConfigureAwait(false))
                .ContinueWith(_ => RunNextLoadTask());
        }

        private static async Task LoadBytes(Uri source)
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(LoadTimeoutMillisec) };
            try
            {
                byte[] result;
                Func<Uri, byte[]> resolver;
                if (source.Scheme != "http" && source.Scheme != "https" &&
                    _specialTable.TryGetValue(source.Scheme, out resolver))
                {
                    result = resolver(source);
                }
                else
                {
                    var errorCount = 0;
                    while (true)
                    {
                        errorCount++;
                        try
                        {
                            using (var response = await client.GetAsync(source).ConfigureAwait(false))
                            {
                                result = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (errorCount > MaxRetryCount)
                            {
                                System.Diagnostics.Debug.WriteLine("could not load:" + source + Environment.NewLine +
                                                                   ex.Message);
                                throw;
                            }
                            System.Diagnostics.Debug.WriteLine(ex.ToString());
                            continue;
                        }
                        break;
                    }
                }
                SetCache(source, result);
                AfterLoadCompleted(source, result);
            }
            catch (Exception)
            {
                // load failed
                CleanInternalTables(source);
            }
            finally
            {
                client.Dispose();
            }
        }

        private static void AfterLoadCompleted(Uri uri, byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                // image is not loaded, or load failed
                CleanInternalTables(uri);
                return;
            }

            HashSet<Guid> set;
            lock (_loadTable)
            {
                if (!_loadTable.TryGetValue(uri, out set))
                {
                    return;
                }
                _loadTable.Remove(uri);
            }

            // ensure synchronized
            lock (set)
            {
                foreach (var id in set)
                {
                    // ensure image uri is not changed
                    Tuple<IImageVisual, Uri, int, int> tuple;
                    if (!_visualTable.TryGetValue(id, out tuple))
                    {
                        continue;
                    }
                    if (tuple.Item1.Id != id || tuple.Item2 != uri)
                    {
                        // remove item
                        _visualTable.TryRemove(id, out tuple);
                    }
                    else
                    {
                        QueueDecodeTask(id, uri, imageBytes,
                            tuple.Item3, tuple.Item4);
                    }
                }
            }
        }

        private static void CleanInternalTables(Uri uri)
        {
            HashSet<Guid> set;
            lock (_loadTable)
            {
                if (!_loadTable.TryGetValue(uri, out set))
                {
                    return;
                }
                _loadTable.Remove(uri);
            }

            // ensure synchronized
            lock (set)
            {
                foreach (var id in set)
                {
                    // if failed, remove from visual table
                    Tuple<IImageVisual, Uri, int, int> tuple;
                    _visualTable.TryRemove(id, out tuple);
                }
            }
        }

        #endregion

        #region Image Decoder

        private static Thread _decoderThread;

        private static void StartDecodeThread()
        {
            if (_decoderThread != null)
            {
                throw new InvalidOperationException("decoder thread is already started.");
            }
            _decoderThread = new Thread(DecodeTaskWorker);
            _decoderThread.Start();
        }

        private static readonly LinkedList<Tuple<Guid, Uri, byte[], int, int>> _decodeStack =
            new LinkedList<Tuple<Guid, Uri, byte[], int, int>>();

        private static readonly ManualResetEventSlim _decodeSignal = new ManualResetEventSlim();

        private static void QueueDecodeTask(Guid targetId,
            Uri uriSource, byte[] bytes, int dpw, int dph)
        {
            lock (_decodeStack)
            {
                var item = Tuple.Create(targetId, uriSource, bytes, dpw, dph);
#pragma warning disable 162
                // ReSharper disable HeuristicUnreachableCode
                Tuple<IImageVisual, Uri, int, int> removal;
                switch (DecodeStrategy)
                {
                    case ImageProcessStrategy.FifoQueue:
                        _decodeStack.AddLast(item);
                        if (_decodeStack.Count > MaxDecodeQueueSize)
                        {
                            _visualTable.TryRemove(_decodeStack.First.Value.Item1, out removal);
                            _decodeStack.RemoveFirst();
                        }
                        break;
                    case ImageProcessStrategy.LifoStack:
                        _decodeStack.AddFirst(item);
                        if (_decodeStack.Count > MaxDecodeQueueSize)
                        {
                            _visualTable.TryRemove(_decodeStack.Last.Value.Item1, out removal);
                            _decodeStack.RemoveLast();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                // ReSharper restore HeuristicUnreachableCode
#pragma warning restore 162
            }
            _decodeSignal.Set();
        }

        private static void DecodeTaskWorker()
        {
            var loop = true;

            // exit handler
            App.ApplicationFinalize += () =>
            {
                loop = false;
                // ReSharper disable once AccessToDisposedClosure
                _decodeSignal.Set();
            };

            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (loop)
            {
                _decodeSignal.Reset();

                while (true)
                {
                    Tuple<Guid, Uri, byte[], int, int> item;
                    lock (_decodeStack)
                    {
                        if (_decodeStack.Count == 0)
                        {
                            break;
                        }
                        item = _decodeStack.First.Value;
                        _decodeStack.RemoveFirst();
                    }

                    // create image
                    var bitmap = CreateImage(item.Item3, item.Item4, item.Item5);

                    // apply image
                    var id = item.Item1;
                    var uri = item.Item2;

                    Tuple<IImageVisual, Uri, int, int> tuple;
                    if (_visualTable.TryRemove(id, out tuple) &&
                        bitmap != null &&
                        tuple.Item1.Id == id &&
                        tuple.Item2 == uri)
                    {
                        // push dispatcher queue
                        ApplyImage(tuple.Item1, uri, bitmap);
                    }

                    // reset signal
                    _decodeSignal.Reset();
                }
                _decodeSignal.Wait();
            }
            // dispose decode signal
            _decodeSignal.Dispose();

            // cleaning up dispatcher.
            DispatcherExtension.BeginInvokeShutdown();
        }

        private static BitmapImage CreateImage(byte[] b, int dpw, int dph)
        {
            try
            {
                using (var ms = new MemoryStream(b, false))
                using (var ws = new WrappingStream(ms))
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ws;
                    if (dpw > 0 || dph > 0)
                    {
                        bi.DecodePixelWidth = dpw;
                        bi.DecodePixelHeight = dph;
                    }
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Apply images on UI thread

        private static bool _waitingDispatcher;

        private static readonly Stack<Tuple<IImageVisual, Uri, BitmapImage>> _applyQueue =
            new Stack<Tuple<IImageVisual, Uri, BitmapImage>>();

        private static void ApplyImage(IImageVisual target, Uri source, BitmapImage image)
        {
            lock (_applyQueue)
            {
                _applyQueue.Push(Tuple.Create(target, source, image));
                if (_waitingDispatcher)
                {
                    return;
                }
                _waitingDispatcher = true;
            }
            // dispatch action
            DispatcherHelper.UIDispatcher.InvokeAsync(ApplyImageOnDispatcher, DispatcherPriority.Loaded);
        }

        private static void ApplyImageOnDispatcher()
        {
            var list = new List<Tuple<IImageVisual, Uri, BitmapImage>>();

            lock (_applyQueue)
            {
                for (var i = 0; i < MaxApplyImagePerOnce && _applyQueue.Count > 0; i++)
                {
                    list.Add(_applyQueue.Pop());
                }
            }

            foreach (var tuple in list)
            {
                tuple.Item1.ApplyImage(tuple.Item2, tuple.Item3);
            }

            lock (_applyQueue)
            {
                if (_applyQueue.Count > 0)
                {
                    // re-dispatch action
                    DispatcherHelper.UIDispatcher.InvokeAsync(ApplyImageOnDispatcher,
                        DispatcherPriority.Loaded);
                }
                else
                {
                    _waitingDispatcher = false;
                }
            }
        }

        #endregion

        #region Wrapping Stream implementation

        // ref:
        // “Memory leak” with BitmapImage and MemoryStream — Logos Bible Software Code Blog
        // http://code.logos.com/blog/2008/04/memory_leak_with_bitmapimage_and_memorystream.html
        private sealed class WrappingStream : Stream
        {
            private Stream _stream;

            public WrappingStream([CanBeNull] Stream stream)
            {
                if (stream == null) throw new ArgumentNullException("stream");
                this._stream = stream;
            }

            public override bool CanRead
            {
                get { return this._stream != null && this._stream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return this._stream != null && this._stream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return this._stream != null && this._stream.CanWrite; }
            }

            public override long Length
            {
                get
                {
                    this.AssertDisposed();
                    return this._stream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    this.AssertDisposed();
                    return this._stream.Position;
                }
                set
                {
                    this.AssertDisposed();
                    this._stream.Position = value;
                }
            }

            public override IAsyncResult BeginRead(
                byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                this.AssertDisposed();
                return this._stream.BeginRead(buffer, offset, count, callback, state);
            }

            public override IAsyncResult BeginWrite(
                byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                this.AssertDisposed();
                return this._stream.BeginWrite(buffer, offset, count, callback, state);
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                this.AssertDisposed();
                return this._stream.EndRead(asyncResult);
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                this.AssertDisposed();
                this._stream.EndWrite(asyncResult);
            }

            public override void Flush()
            {
                this.AssertDisposed();
                this._stream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                this.AssertDisposed();
                return this._stream.Read(buffer, offset, count);
            }

            public override int ReadByte()
            {
                this.AssertDisposed();
                return this._stream.ReadByte();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                this.AssertDisposed();
                return this._stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                this.AssertDisposed();
                this._stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                this.AssertDisposed();
                this._stream.Write(buffer, offset, count);
            }

            public override void WriteByte(byte value)
            {
                this.AssertDisposed();
                this._stream.WriteByte(value);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this._stream.Dispose();
                    this._stream = null;
                }
                base.Dispose(disposing);
            }

            private void AssertDisposed()
            {
                if (this._stream == null)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
        }

        #endregion

        private static readonly ConcurrentDictionary<Guid, Tuple<IImageVisual, Uri, int, int>> _visualTable =
            new ConcurrentDictionary<Guid, Tuple<IImageVisual, Uri, int, int>>();

        public static void QueueLoadImage(IImageVisual image, Uri uri, int dpw, int dph)
        {
            var id = image.Id;
            if (id == Guid.Empty) return;
            _visualTable[id] = Tuple.Create(image, uri, dpw, dph);

            // load image
            byte[] cache;
            if (GetCache(uri, out cache))
            {
                // decode immediately
                QueueDecodeTask(id, uri, cache, dpw, dph);
            }
            else
            {
                QueueLoadTask(uri, id);
            }
        }

        public static void CancelLoading(Guid id)
        {
            Tuple<IImageVisual, Uri, int, int> tuple;
            if (id == Guid.Empty) return;
            if (!_visualTable.TryRemove(id, out tuple))
            {
                return;
            }
            var removalUri = tuple.Item2;
            // remove from loader stack
            lock (_loadTable)
            {
                HashSet<Guid> set;
                if (!_loadTable.TryGetValue(removalUri, out set))
                {
                    return;
                }
                lock (set)
                {
                    set.Remove(id);
                }
            }
        }
    }

    public interface IImageVisual
    {
        Guid Id { get; }

        void ApplyImage(Uri sourceUri, BitmapImage image);
    }

    public enum ImageProcessStrategy
    {
        FifoQueue,
        LifoStack,
    }
}
