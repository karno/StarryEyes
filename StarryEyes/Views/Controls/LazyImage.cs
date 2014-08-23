using System;
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

        private static void ImagePropertyChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue) return;
            var img = sender as LazyImage;
            if (img == null) return;
            img.UpdateImage();
        }

        public Guid Id { get; private set; }

        private Uri _lastUri;
        private int _lastDpw;
        private int _lastDph;

        public LazyImage()
        {
            this.Id = Guid.Empty;
            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Detach();
            if (this.UriSource != null)
            {
                UpdateImage();
            }
        }

        private void UpdateImage()
        {
            if (!this.IsLoaded) return;

            // get parameters
            var uri = this.UriSource;
            var dpw = this.DecodePixelWidth;
            var dph = this.DecodePixelHeight;
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
                this.Source = null;

                if (uri == null) return;

                // load image
                if (uri.Scheme == "pack")
                {
                    // image is PACK image
                    var bi = new BitmapImage(uri) { CacheOption = BitmapCacheOption.OnLoad };
                    bi.Freeze();
                    this.ApplyImage(uri, bi);
                }
                else
                {
                    Id = Guid.NewGuid();
                    // resolve from ImageLoader
                    ImageLoader.QueueLoadImage(this, uri, dpw, dph);
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
            if (this.Id == Guid.Empty) return;
            var pid = this.Id;
            this.Id = Guid.Empty;
            ImageLoader.CancelLoading(pid);
        }

        public void ApplyImage(Uri sourceUri, BitmapImage image)
        {
            if (this.UriSource != sourceUri) return;
            try
            {
                this.Source = image;
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
        }
    }
}