using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using StarryEyes.Annotations;

namespace StarryEyes.Views.Controls
{
    public class LazyImage : Image
    {
        static LazyImage()
        {
            Observable.Timer(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5))
                      .Subscribe(_ => RemoveExpiredCaches());
        }

        #region Constant variables

        private const int MaxRetryCount = 3;
        private const int MaxParallelism = 8;

        #endregion

        #region Dependency properties

        public static readonly DependencyProperty UriSourceProperty =
            DependencyProperty.Register("UriSource", typeof(Uri), typeof(LazyImage),
                new PropertyMetadata(null, ImagePropertyChanged));

        public Uri UriSource
        {
            get { return (Uri)GetValue(UriSourceProperty); }
            set { SetValue(UriSourceProperty, value); }
        }

        public static readonly DependencyProperty DecodePixelWidthProperty =
            DependencyProperty.Register("DecodePixelWidth", typeof(int), typeof(LazyImage),
                new PropertyMetadata(0, ImagePropertyChanged));

        public int DecodePixelWidth
        {
            get { return (int)GetValue(DecodePixelWidthProperty); }
            set { SetValue(DecodePixelWidthProperty, value); }
        }

        public static readonly DependencyProperty DecodePixelHeightProperty =
            DependencyProperty.Register("DecodePixelHeight", typeof(int), typeof(LazyImage),
                new PropertyMetadata(0, ImagePropertyChanged));

        public int DecodePixelHeight
        {
            get { return (int)GetValue(DecodePixelHeightProperty); }
            set { SetValue(DecodePixelHeightProperty, value); }
        }

        #endregion

        #region Image cache

        private static readonly int MaxCacheCount = 512;

        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

        private static readonly LinkedList<Tuple<Uri, byte[], DateTime>> _cacheList =
            new LinkedList<Tuple<Uri, byte[], DateTime>>();

        private static readonly Dictionary<Uri, LinkedListNode<Tuple<Uri, byte[], DateTime>>> _cacheTable =
            new Dictionary<Uri, LinkedListNode<Tuple<Uri, byte[], DateTime>>>();

        private static bool GetCache([NotNull] Uri uri, out byte[] cache)
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

        private static void SetCache([NotNull] Uri uri, [NotNull]byte[] imageByte)
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
            GC.Collect();
        }

        #endregion

        #region Image Loader Thread

        private static readonly ConcurrentStack<Tuple<Uri, Subject<byte[]>>> _workStack =
            new ConcurrentStack<Tuple<Uri, Subject<byte[]>>>();

        private static int _concurrency;

        private static void PushTask(Uri source, Subject<byte[]> resultSubject)
        {
            _workStack.Push(Tuple.Create(source, resultSubject));
            if (Interlocked.Increment(ref _concurrency) > MaxParallelism)
            {
                Interlocked.Decrement(ref _concurrency);
                return;
            }
            // run task
            RunNextTask();
        }

        private static void RunNextTask()
        {
            Tuple<Uri, Subject<byte[]>> item;
            if (!_workStack.TryPop(out item))
            {
                Interlocked.Decrement(ref _concurrency);
            }
            else
            {
                Task.Run(async () => await LoadBytes(item.Item1, item.Item2))
                    .ContinueWith(_ => RunNextTask());
            }
        }

        private static async Task LoadBytes(Uri source, Subject<byte[]> subject)
        {
            var client = new HttpClient();
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
                            using (var response = await client.GetAsync(source))
                            {
                                result = await response.Content.ReadAsByteArrayAsync();
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
                lock (_imageObservables)
                {
                    _imageObservables.Remove(source);
                }
                subject.OnNext(result);
                subject.OnCompleted();
            }
            catch (Exception ex)
            {
                subject.OnError(ex);
                subject.Dispose();
            }
            finally
            {
                client.Dispose();
            }
        }

        #endregion

        private static readonly Dictionary<Uri, IObservable<byte[]>> _imageObservables =
            new Dictionary<Uri, IObservable<byte[]>>();

        private static void ImagePropertyChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue) return;
            var img = sender as LazyImage;
            if (img == null) return;
            var uri = img.UriSource;
            var dpw = img.DecodePixelWidth;
            var dph = img.DecodePixelHeight;
            System.Diagnostics.Debug.WriteLine("IMAGE DECODE:: URI:" + uri + " / DPW:" + dpw + " / DPH:" + dph + " / NVAL:" + e.NewValue);
            SetImage(img, null, null);
            if (uri != null)
            {
                ReloadImage(img, uri, dpw, dph);
            }
        }

        private static void ReloadImage(LazyImage img, Uri uri, int dpw, int dph)
        {
            try
            {
                if (uri.Scheme == "pack")
                {
                    // PACK image
                    var bi = new BitmapImage(uri) { CacheOption = BitmapCacheOption.OnLoad };
                    bi.Freeze();
                    SetImage(img, bi, uri);
                }
                else
                {
                    byte[] cache;
                    if (GetCache(uri, out cache))
                    {
                        // decode image
                        Task.Run(() =>
                        {
                            var b = CreateImage(cache, dpw, dph);
                            DispatcherHolder.Enqueue(() => SetImage(img, b, uri), DispatcherPriority.Loaded);
                        });
                        return;
                    }
                    img.Source = null;
                    IObservable<byte[]> observable;
                    Subject<byte[]> publisher = null;
                    lock (_imageObservables)
                    {
                        if (!_imageObservables.TryGetValue(uri, out observable))
                        {
                            observable = publisher = new Subject<byte[]>();
                            _imageObservables.Add(uri, publisher);
                        }
                    }
                    observable.Select(b => CreateImage(b, dpw, dph))
                              .Subscribe(b => DispatcherHolder.Enqueue(
                                  () => SetImage(img, b, uri), DispatcherPriority.Loaded)
                                  , ex => { });
                    if (publisher != null)
                    {
                        PushTask(uri, publisher);
                    }
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {
            }
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

        private static void SetImage(LazyImage image, ImageSource source, Uri sourceFrom)
        {
            try
            {
                if (image.UriSource != sourceFrom) return;
                if (source == null)
                {
                    // unset value
                    image.Source = null;
                    return;
                }
                if (!source.IsFrozen)
                {
                    throw new ArgumentException("Image is not frozen.");
                }
                image.Source = source;
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {
            }
        }

        #region Special resolver table

        private static readonly ConcurrentDictionary<string, Func<Uri, byte[]>> _specialTable =
            new ConcurrentDictionary<string, Func<Uri, byte[]>>();

        public static bool RegisterSpecialResolverTable(string scheme, Func<Uri, byte[]> resolver)
        {
            return _specialTable.TryAdd(scheme, resolver);
        }

        #endregion

        // ref:
        // “Memory leak” with BitmapImage and MemoryStream — Logos Bible Software Code Blog
        // http://code.logos.com/blog/2008/04/memory_leak_with_bitmapimage_and_memorystream.html
        private sealed class WrappingStream : Stream
        {
            private Stream _stream;

            public WrappingStream([NotNull] Stream stream)
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
    }
}