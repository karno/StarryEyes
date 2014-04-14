using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Helpers;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Requests;
using StarryEyes.Settings;

namespace StarryEyes.Models.Inputting
{
    public class InputData
    {
        private TwitterAccount[] _accounts;
        private string _initText;
        private string _text;
        private byte[] _attachedImage;
        private GeoLocationInfo _geoInfo;
        private string[] _boundTags;
        private Dictionary<TwitterAccount, TwitterStatus> _amendTweets =
            new Dictionary<TwitterAccount, TwitterStatus>();
        private TwitterStatus _inReplyTo;
        private TwitterUser _messageRecipient;

        public InputData(string text)
        {
            // initialize text.
            _initText = _text = text ?? String.Empty;
        }

        #region Constant properties

        public bool IsTagBindEnabled
        {
            get
            {
                // suppress tag binding
                // when amending tweet,
                // sending direct message, or
                // replying someone (if configured so that).
                return !this.IsAmend && !this.IsDirectMessage &&
                       (!Setting.SuppressTagBindingInReply.Value || this._inReplyTo == null);
            }
        }

        public bool IsDirectMessage
        {
            get { return _messageRecipient != null; }
        }

        public bool IsChanged
        {
            get
            {
                return AttachedImage != null ||
                       _text != _initText && !String.IsNullOrEmpty(
                           _text.Replace("\t", "")
                                .Replace("\r", "")
                                .Replace("\n", "")
                                .Replace(" ", "")
                                .Replace("　", ""));
            }
        }

        public bool IsAmend
        {
            get { return _amendTweets.Count > 0; }
        }

        #endregion

        #region Accessing properties

        [CanBeNull]
        public IEnumerable<TwitterAccount> Accounts
        {
            get { return _accounts; }
            set { _accounts = value == null ? null : value.ToArray(); }
        }

        [NotNull]
        public IEnumerable<string> BoundTags
        {
            get
            {
                return IsDirectMessage && _boundTags != null
                           ? _boundTags
                           : Enumerable.Empty<string>();
            }
            set
            {
                var array = value.Guard().ToArray();
                if (array.Length > 0 && IsDirectMessage)
                {
                    throw new InvalidOperationException(
                        "Could not bind tags when InputData is in DirectMessage mode.");
                }
                _boundTags = IsDirectMessage
                                 ? new string[0]
                                 : value.Guard().ToArray();
            }
        }

        [CanBeNull]
        public TwitterStatus InReplyTo
        {
            get { return _inReplyTo; }
            set
            {
                if (value != null)
                {
                    if (this.IsDirectMessage)
                    {
                        throw new InvalidOperationException(
                            "Could not set InReplyTo when InputData is in DirectMessage mode.");
                    }
                    if (value.StatusType == StatusType.DirectMessage)
                    {
                        throw new ArgumentException("Could not set direct message as reply target.");
                    }
                }
                _inReplyTo = value;
            }
        }

        [CanBeNull]
        public TwitterUser MessageRecipient
        {
            get { return _messageRecipient; }
            set
            {
                if (InReplyTo != null)
                {
                    throw new InvalidOperationException(
                        "Could not set MessageRecipient when InReplyTo is already set.");
                }
                if (AttachedGeoLocation != null || AttachedImage != null)
                {
                    throw new InvalidOperationException(
                        "Could not set MessageRecipient when image or location is attached.");
                }
                _messageRecipient = value;
            }
        }

        [NotNull]
        public string Text
        {
            get { return _text; }
            // ReSharper disable ConstantNullCoalescingCondition
            set { _text = value ?? String.Empty; }
            // ReSharper restore ConstantNullCoalescingCondition
        }

        [NotNull]
        public IEnumerable<KeyValuePair<TwitterAccount, TwitterStatus>> AmendTargetTweets
        {
            get { return _amendTweets.ToArray(); }
            set { _amendTweets = value.Guard().ToDictionary(kvp => kvp.Key, kvp => kvp.Value); }
        }

        [CanBeNull]
        public GeoLocationInfo AttachedGeoLocation
        {
            get { return _geoInfo; }
            set
            {
                if (value != null && IsDirectMessage)
                {
                    throw new InvalidOperationException(
                        "Could not attach geo info when InputData is in DirectMessage mode.");
                }
                _geoInfo = value;
            }
        }

        [CanBeNull]
        public byte[] AttachedImage
        {
            get { return _attachedImage; }
            set
            {
                if (value != null)
                {
                    if (IsDirectMessage)
                    {
                        throw new InvalidOperationException(
                            "Could not attach image when InputData is in DirectMessage mode.");
                    }
                }
                _attachedImage = value;
            }
        }

        #endregion

        [NotNull]
        private InputData Clone()
        {
            return new InputData(_initText)
            {
                _accounts = _accounts == null ? null : _accounts.ToArray(),
                _amendTweets = _amendTweets.ToDictionary(p => p.Key, p => p.Value),
                _attachedImage = _attachedImage,
                _boundTags = _boundTags,
                _geoInfo = _geoInfo,
                _inReplyTo = _inReplyTo,
                _initText = _initText,
                _messageRecipient = _messageRecipient,
                _text = _text
            };
        }

        [NotNull]
        public IObservable<PostResult> Send()
        {
            var existedTags = TwitterRegexPatterns.ValidHashtag.Matches(Text)
                                                  .OfType<Match>()
                                                  .Select(_ => _.Groups[1].Value)
                                                  .Distinct()
                                                  .ToArray();
            var binds = !IsTagBindEnabled
                ? String.Empty
                : _boundTags.Guard().Except(existedTags)
                            .Distinct()
                            .Select(t => " #" + t)
                            .JoinString(String.Empty);
            RequestBase<TwitterStatus> request;
            if (IsDirectMessage)
            {
                request = new MessagePostingRequest(MessageRecipient, Text);
            }
            else
            {
                request = new TweetPostingRequest(Text + binds, InReplyTo,
                    AttachedGeoLocation, _attachedImage);
            }
            var s = Observable.Defer(() => Observable.Start(() => _accounts.Guard().ToObservable()))
                              .SelectMany(a => a)
                              .SelectMany(a => SendInternal(a, request))
                              .WaitForCompletion()
                              .Select(r => r.ToLookup(t => t.Item3 == null))
                              .Select(g =>
                              {
                                  InputData succ = null;
                                  InputData fail = null;
                                  Exception[] exs = null;
                                  if (g.Contains(true))
                                  {
                                      succ = this.Clone();
                                      // succeeded results
                                      var succeeds = g[true].ToArray();
                                      succ.AmendTargetTweets = succeeds.ToDictionary(t => t.Item1, t => t.Item2);
                                      succ.Accounts = succeeds.Select(t => t.Item1);
                                  }
                                  if (g.Contains(false))
                                  {
                                      fail = this.Clone();
                                      // failed results
                                      var faileds = g[false].ToArray();
                                      fail.Accounts = faileds.Select(t => t.Item1);
                                      exs = faileds.Select(t => t.Item3).ToArray();
                                  }
                                  return new PostResult(succ, fail, exs);
                              });
            if (IsAmend)
            {
                return AmendTargetTweets
                    .ToObservable()
                    .SelectMany(t => RequestQueue.Enqueue(t.Key, new DeletionRequest(t.Value)))
                    .Select(_ => (PostResult)null)
                    .Concat(s)
                    .Where(r => r != null);
            }
            return s;
        }

        [NotNull]
        private IObservable<Tuple<TwitterAccount, TwitterStatus, Exception>> SendInternal(
            [NotNull] TwitterAccount account, [NotNull] RequestBase<TwitterStatus> request)
        {
            if (account == null) throw new ArgumentNullException("account");
            if (request == null) throw new ArgumentNullException("request");
            return RequestQueue.Enqueue(account, request)
                               .Do(StatusInbox.Enqueue)
                               .Select(s => Tuple.Create(account, s, (Exception)null))
                               .Catch((Exception ex) =>
                                   Observable.Return(Tuple.Create(account, (TwitterStatus)null, ex)));
        }
    }

    public class PostResult
    {
        public PostResult(InputData succeededs, InputData faileds, IEnumerable<Exception> throwns)
        {
            Succeededs = succeededs;
            Faileds = faileds;
            Exceptions = throwns;
        }

        [CanBeNull]
        public InputData Succeededs { get; private set; }

        [CanBeNull]
        public InputData Faileds { get; private set; }

        [CanBeNull]
        public IEnumerable<Exception> Exceptions { get; private set; }
    }

    public enum ImageType
    {
        Bmp,
        Gif,
        Jpg,
        Png,
        Tiff,
    }

    public static class ImageUtil
    {
        public static BitmapImage CreateImage(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.None;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }

        public static byte[] SaveToBytes(this BitmapSource image, ImageType saveType = ImageType.Png)
        {
            BitmapEncoder encoder;
            switch (saveType)
            {
                case ImageType.Gif:
                    encoder = new GifBitmapEncoder();
                    break;
                case ImageType.Jpg:
                    encoder = new JpegBitmapEncoder();
                    break;
                case ImageType.Tiff:
                    encoder = new TiffBitmapEncoder();
                    break;
                default:
                    // default: use PNG format
                    encoder = new PngBitmapEncoder();
                    break;
            }
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        public static ImageType DetermineImageType(byte[] image)
        {
            using (Stream ms = new MemoryStream(image))
            {
                var table = new Dictionary<Type, ImageType>
                {
                    {typeof (BmpBitmapDecoder), ImageType.Bmp},
                    {typeof (GifBitmapDecoder), ImageType.Gif},
                    {typeof (JpegBitmapDecoder), ImageType.Jpg},
                    {typeof (PngBitmapDecoder), ImageType.Png},
                    {typeof (TiffBitmapDecoder), ImageType.Tiff},
                };
                var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.None);
                return table.Where(imageType => decoder.GetType() == imageType.Key)
                            .Select(imageType => imageType.Value)
                            .FirstOrDefault();
            }
        }
    }
}
