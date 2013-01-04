using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
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

        private static void UriSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var img = sender as LazyImage;
            if (img == null) return;
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
                        Subject<byte[]> publisher = null;
                        _imageStreamer.GetOrAdd(uri, _ => publisher = new Subject<byte[]>())
                                      .Select(b => new MemoryStream(b, false))
                                      .Select(ms =>
                                      {
                                          var bi = new BitmapImage();
                                          bi.BeginInit();
                                          bi.CacheOption = BitmapCacheOption.OnLoad;
                                          bi.StreamSource = ms;
                                          bi.EndInit();
                                          bi.Freeze();
                                          return bi;
                                      })
                                      .ObserveOnDispatcher()
                                      .Subscribe(b => SetImage(img, b, uri));
                        if (publisher != null)
                        {
                            var wc = new WebClient();
                            wc.DownloadDataTaskAsync(uri)
                              .ToObservable()
                              .Finally(() =>
                              {
                                  IObservable<byte[]> subscribe;
                                  _imageStreamer.TryRemove(uri, out subscribe);
                              })
                              .Subscribe(publisher.OnNext, ex => { });
                        }
                    }
                }
                catch
                {
                }
            }
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
                    System.Diagnostics.Debug.WriteLine("set " + sourceFrom);
                }
            }
            catch
            {
            }
        }
    }
}