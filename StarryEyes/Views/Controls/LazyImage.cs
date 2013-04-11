using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StarryEyes.Views.Controls
{
    public class LazyImage : Image
    {
        // Using a DependencyProperty as the backing store for UriSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UriSourceProperty =
            DependencyProperty.Register("UriSource", typeof(Uri), typeof(LazyImage),
                                        new PropertyMetadata(null, UriSourcePropertyChanged));

        private static readonly ConcurrentDictionary<Uri, IObservable<byte[]>> _imageStreamer =
            new ConcurrentDictionary<Uri, IObservable<byte[]>>();

        public Uri UriSource
        {
            get { return (Uri)GetValue(UriSourceProperty); }
            set { SetValue(UriSourceProperty, value); }
        }

        public int DecodePixelWidth
        {
            get { return (int)GetValue(DecodePixelWidthProperty); }
            set { SetValue(DecodePixelWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DecodePixelWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DecodePixelWidthProperty =
            DependencyProperty.Register("DecodePixelWidth", typeof(int), typeof(LazyImage), new PropertyMetadata(0));

        public int DecodePixelHeight
        {
            get { return (int)GetValue(DecodePixelHeightProperty); }
            set { SetValue(DecodePixelHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DecodePixelHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DecodePixelHeightProperty =
            DependencyProperty.Register("DecodePixelHeight", typeof(int), typeof(LazyImage), new PropertyMetadata(0));

        private static async void UriSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var img = sender as LazyImage;
            if (img == null) return;
            var dcw = img.DecodePixelWidth;
            var dch = img.DecodePixelHeight;
            var uri = e.NewValue as Uri;
            if (e.NewValue == e.OldValue) return;
            if (uri != null)
            {
                try
                {
                    if (uri.Scheme == "pack")
                    {
                        var bi = new BitmapImage(uri) { CacheOption = BitmapCacheOption.OnLoad };
                        bi.Freeze();
                        SetImage(img, bi, uri);
                    }
                    else
                    {
                        img.Source = null;
                        Subject<byte[]> publisher = null;
                        _imageStreamer.GetOrAdd(uri, _ => publisher = new Subject<byte[]>())
                                      .Select(b => new MemoryStream(b, false))
                                      .Select(ms =>
                                      {
                                          try
                                          {
                                              var bi = new BitmapImage();
                                              bi.BeginInit();
                                              bi.CacheOption = BitmapCacheOption.OnLoad;
                                              bi.StreamSource = ms;
                                              bi.DecodePixelWidth = dcw;
                                              bi.DecodePixelHeight = dch;
                                              bi.EndInit();
                                              bi.Freeze();
                                              ms.Dispose();
                                              return bi;
                                          }
                                          catch
                                          {
                                              return null;
                                          }
                                      })
                                      .ObserveOnDispatcher()
                                      .Subscribe(b => SetImage(img, b, uri), ex => { });
                        if (publisher != null)
                        {
                            WaitObjects.Enqueue(Tuple.Create(uri, publisher));
                            while (Interlocked.Increment(ref _threadCount) > ThreadMaxCount)
                            {
                                if (Interlocked.Decrement(ref _threadCount) > 0)
                                    return;
                            }
                            await Worker();
                            Interlocked.Decrement(ref _threadCount);
                        }
                    }
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch
                // ReSharper restore EmptyGeneralCatchClause
                {
                }
            }
        }

        private const int MaxRetryCount = 3;
        private const int ThreadMaxCount = 8;
        private static int _threadCount;
        private static readonly ConcurrentQueue<Tuple<Uri, Subject<byte[]>>> WaitObjects = new ConcurrentQueue<Tuple<Uri, Subject<byte[]>>>();

        private static async Task Worker()
        {
            await Task.Run(() =>
            {
                Tuple<Uri, Subject<byte[]>> dequeue;
                var client = new WebClient();
                while (WaitObjects.TryDequeue(out dequeue))
                {
                    try
                    {
                        byte[] result;
                        Func<Uri, byte[]> resolver;
                        var uri = dequeue.Item1;
                        if (uri.Scheme != "http" && uri.Scheme != "https" &&
                            _specialTable.TryGetValue(uri.Scheme, out resolver))
                        {
                            result = resolver(uri);
                        }
                        else
                        {
                            int errorCount = 0;
                            while (true)
                            {
                                errorCount++;
                                try
                                {
                                    result = client.DownloadData(uri);
                                }
                                catch (Exception ex)
                                {
                                    if (errorCount > MaxRetryCount)
                                    {
                                        throw;
                                    }
                                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                                    continue;
                                }
                                break;
                            }
                        }
                        IObservable<byte[]> removal;
                        _imageStreamer.TryRemove(uri, out removal);
                        dequeue.Item2.OnNext(result);
                        dequeue.Item2.OnCompleted();
                        dequeue.Item2.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (dequeue == null) continue;
                        dequeue.Item2.OnError(ex);
                        dequeue.Item2.Dispose();
                    }
                }
            });
        }

        private static void SetImage(LazyImage image, ImageSource source, Uri sourceFrom)
        {
            try
            {
                if (source != null && image.UriSource == sourceFrom)
                {
                    if (!source.IsFrozen)
                        throw new ArgumentException("Image is not frozen.");
                    image.Source = source;
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {
            }
        }

        private static readonly ConcurrentDictionary<string, Func<Uri, byte[]>> _specialTable =
            new ConcurrentDictionary<string, Func<Uri, byte[]>>();

        public static bool RegisterSpecialResolverTable(string scheme, Func<Uri, byte[]> resolver)
        {
            return _specialTable.TryAdd(scheme, resolver);
        }
    }
}