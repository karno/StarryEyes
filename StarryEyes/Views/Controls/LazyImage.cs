using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace StarryEyes.Views.Controls
{
    public class LazyImage : Image, IImageVisual
    {
        #region Dependency properties

        public static readonly DependencyProperty UriSourceProperty =
            DependencyProperty.Register("UriSource", typeof(Uri), typeof(LazyImage),
                new PropertyMetadata(null, ImagePropertyChanged));

        public Uri UriSource
        {
            get => (Uri)GetValue(UriSourceProperty);
            set => SetValue(UriSourceProperty, value);
        }

        public static readonly DependencyProperty DecodePixelWidthProperty =
            DependencyProperty.Register("DecodePixelWidth", typeof(int), typeof(LazyImage),
                new PropertyMetadata(0, ImagePropertyChanged));

        public int DecodePixelWidth
        {
            get => (int)GetValue(DecodePixelWidthProperty);
            set => SetValue(DecodePixelWidthProperty, value);
        }

        public static readonly DependencyProperty DecodePixelHeightProperty =
            DependencyProperty.Register("DecodePixelHeight", typeof(int), typeof(LazyImage),
                new PropertyMetadata(0, ImagePropertyChanged));

        public int DecodePixelHeight
        {
            get => (int)GetValue(DecodePixelHeightProperty);
            set => SetValue(DecodePixelHeightProperty, value);
        }

        #endregion Dependency properties

        private static void ImagePropertyChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue) return;
            var img = sender as LazyImage;
            img?.UpdateImage();
        }

        public Guid Id { get; private set; }

        private Uri _lastUri;
        private int _lastDpw;
        private int _lastDph;

        public LazyImage()
        {
            Id = Guid.Empty;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Detach();
            if (UriSource != null)
            {
                UpdateImage();
            }
        }

        private void UpdateImage()
        {
            if (!IsLoaded) return;

            // get parameters
            var uri = UriSource;
            var dpw = DecodePixelWidth;
            var dph = DecodePixelHeight;
            if (uri == _lastUri &&
                dpw == _lastDpw &&
                dph == _lastDph)
            {
                // parameters are not changed.
                return;
            }

            _lastUri = uri;
            _lastDpw = dpw;
            _lastDph = dph;

            // detach previous id
            Detach();

            try
            {
                // clear image
                Source = null;

                if (uri == null) return;

                // load image
                if (uri.Scheme == "pack")
                {
                    // image is PACK image
                    var bi = new BitmapImage(uri) { CacheOption = BitmapCacheOption.OnLoad };
                    bi.Freeze();
                    ApplyImage(uri, bi);
                }
                else
                {
                    Id = Guid.NewGuid();
                    // resolve from ImageLoader
                    Task.Run(() => ImageLoader.QueueLoadImage(this, uri, dpw, dph));
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
                // ReSharper restore EmptyGeneralCatchClause
            {
            }
        }

        void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Detach();
        }

        private void Detach()
        {
            if (Id == Guid.Empty) return;
            var pid = Id;
            Id = Guid.Empty;
            ImageLoader.CancelLoading(pid);
        }

        public void ApplyImage(Uri sourceUri, BitmapImage image)
        {
            if (UriSource != sourceUri) return;
            try
            {
                Source = image;
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }
    }
}