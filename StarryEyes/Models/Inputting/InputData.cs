using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
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
        public async Task<PostResult> SendAsync()
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

            var posts = _accounts.Guard()
                                 .Select(a => Tuple.Create(a, SendInternal(a, request)))
                                 .ToArray();
            var tasks = posts.Select(p => p.Item2).OfType<Task>().ToArray();
            // wait for completion
            Task.WaitAll(tasks);

            var amendTargets = new Dictionary<TwitterAccount, TwitterStatus>();
            var failedAccounts = new List<TwitterAccount>();
            var exceptions = new List<Exception>();

            foreach (var item in posts)
            {
                var account = item.Item1;
                try
                {
                    var result = await item.Item2;
                    amendTargets.Add(account, result);
                }
                catch (Exception ex)
                {
                    failedAccounts.Add(account);
                    exceptions.Add(ex);
                }
            }
            InputData successData = null, failedData = null;

            if (amendTargets.Count > 0)
            {
                successData = this.Clone();
                successData.AmendTargetTweets = amendTargets.ToArray();
                successData.Accounts = amendTargets.Select(p => p.Key);
            }

            if (failedAccounts.Count > 0)
            {
                failedData = this.Clone();
                failedData.Accounts = failedAccounts.ToArray();
            }

            var pr = new PostResult(successData, failedData, exceptions.ToArray());


            if (IsAmend)
            {
                var amends = AmendTargetTweets
                    .Select(pair => RequestQueue.EnqueueAsync(pair.Key, new DeletionRequest(pair.Value)))
                    .OfType<Task>().ToArray();
                Task.WaitAll(amends);
            }
            return pr;
        }

        [NotNull]
        private async Task<TwitterStatus> SendInternal(
            [NotNull] TwitterAccount account, [NotNull] RequestBase<TwitterStatus> request)
        {
            if (account == null) throw new ArgumentNullException("account");
            if (request == null) throw new ArgumentNullException("request");
            return await Task.Run(async () =>
            {
                var status = await RequestQueue.EnqueueAsync(account, request);
                StatusInbox.Enqueue(status);
                return status;
            });
        }
    }

    public class PostResult
    {
        public PostResult([CanBeNull] InputData succeededs, [CanBeNull] InputData faileds,
            [NotNull] IEnumerable<Exception> throwns)
        {
            if (throwns == null) throw new ArgumentNullException("throwns");
            Succeededs = succeededs;
            Faileds = faileds;
            Exceptions = throwns;
        }

        [CanBeNull]
        public InputData Succeededs { get; private set; }

        [CanBeNull]
        public InputData Faileds { get; private set; }

        [NotNull]
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
        [CanBeNull]
        public static BitmapImage CreateImage([CanBeNull] byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            try
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
            catch (Exception ex)
            {
                BehaviorLogger.Log("Image util",
                    "Fail to load image: " + Environment.NewLine + ex);
                return null;
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
