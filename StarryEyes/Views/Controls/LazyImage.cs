using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using StarryEyes.Albireo.Threading;
using StarryEyes.Annotations;

namespace StarryEyes.Views.Controls
{
    public class LazyImage : Image
    {
        #region Constant variables

        private const int MaxRetryCount = 3;
        private const int MaxParallelism = 16;

        #endregion

        #region Dependency properties

        public static readonly DependencyProperty UriSourceProperty =
            DependencyProperty.Register("UriSource", typeof(Uri), typeof(LazyImage),
                                        new PropertyMetadata(null, UriSourcePropertyChanged));

        public Uri UriSource
        {
            get { return (Uri)GetValue(UriSourceProperty); }
            set { SetValue(UriSourceProperty, value); }
        }

        public static readonly DependencyProperty DecodePixelWidthProperty =
            DependencyProperty.Register("DecodePixelWidth", typeof(int), typeof(LazyImage), new PropertyMetadata(0));

        public int DecodePixelWidth
        {
            get { return (int)GetValue(DecodePixelWidthProperty); }
            set { SetValue(DecodePixelWidthProperty, value); }
        }

        public static readonly DependencyProperty DecodePixelHeightProperty =
            DependencyProperty.Register("DecodePixelHeight", typeof(int), typeof(LazyImage), new PropertyMetadata(0));

        public int DecodePixelHeight
        {
            get { return (int)GetValue(DecodePixelHeightProperty); }
            set { SetValue(DecodePixelHeightProperty, value); }
        }

        #endregion

        private static readonly ConcurrentDictionary<Uri, IObservable<byte[]>> _imageStreamer =
            new ConcurrentDictionary<Uri, IObservable<byte[]>>();

        private static readonly TaskFactory _taskFactory = LimitedTaskScheduler.GetTaskFactory(MaxParallelism);

        private static void UriSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var img = sender as LazyImage;
            if (img == null) return;
            var dpw = img.DecodePixelWidth;
            var dph = img.DecodePixelHeight;
            var uri = e.NewValue as Uri;
            if (e.NewValue == e.OldValue || uri == null) return;
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
                    img.Source = null;
                    Subject<byte[]> publisher = null;
                    _imageStreamer.GetOrAdd(uri, _ => publisher = new Subject<byte[]>())
                                  .Select(b =>
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
                                  })
                                  .Subscribe(b => DispatcherHolder.Enqueue(
                                      () => SetImage(img, b, uri), DispatcherPriority.Loaded)
                                             , ex => { });
                    if (publisher != null)
                    {
                        _taskFactory.StartNew(() => LoadBytes(uri, publisher));
                    }
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {
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
                IObservable<byte[]> removal;
                _imageStreamer.TryRemove(source, out removal);
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

        private static void SetImage(LazyImage image, ImageSource source, Uri sourceFrom)
        {
            try
            {
                if (source == null || image.UriSource != sourceFrom) return;
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