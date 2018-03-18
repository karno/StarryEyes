using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Cadena.Api.Parameters;
using Cadena.Data;
using Cadena.Engine.Requests;
using JetBrains.Annotations;
using Livet;
using StarryEyes.Helpers;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Subsystems;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Inputting;

namespace StarryEyes.Models.Inputting
{
    public class InputData
    {
        private readonly DispatcherCollection<byte[]> _attachedImages =
            new DispatcherCollection<byte[]>(DispatcherHelper.UIDispatcher);

        private TwitterAccount[] _accounts;
        private string _initText;
        private string _text;
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

        // suppress tag binding
        // when amending tweet,
        // sending direct message, or
        // replying someone (if configured so that).
        public bool IsTagBindEnabled => !IsAmend && !IsDirectMessage &&
                                        (!Setting.SuppressTagBindingInReply.Value || _inReplyTo == null);

        public bool IsDirectMessage => _messageRecipient != null;

        public bool IsChanged => _attachedImages?.Count > 0 ||
                                 _text != _initText && !String.IsNullOrEmpty(
                                     _text.Replace("\t", "")
                                          .Replace("\r", "")
                                          .Replace("\n", "")
                                          .Replace(" ", "")
                                          .Replace("　", ""));

        public bool IsAmend => _amendTweets.Count > 0;

        #endregion Constant properties

        #region Accessing properties

        [CanBeNull]
        public IEnumerable<TwitterAccount> Accounts
        {
            get => _accounts;
            set => _accounts = value?.ToArray();
        }

        [CanBeNull]
        public IEnumerable<string> BoundTags
        {
            get => IsDirectMessage && _boundTags != null
                ? _boundTags
                : Enumerable.Empty<string>();
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
            get => _inReplyTo;
            set
            {
                if (value != null)
                {
                    if (IsDirectMessage)
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
            get => _messageRecipient;
            set
            {
                if (InReplyTo != null)
                {
                    throw new InvalidOperationException(
                        "Could not set MessageRecipient when InReplyTo is already set.");
                }
                if (AttachedGeoLocation != null || (AttachedImages != null && AttachedImages.Count > 0))
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
            get => _text ?? String.Empty;
            // ReSharper disable ConstantNullCoalescingCondition
            set => _text = value ?? String.Empty;
            // ReSharper restore ConstantNullCoalescingCondition
        }

        [CanBeNull]
        public IEnumerable<KeyValuePair<TwitterAccount, TwitterStatus>> AmendTargetTweets
        {
            get => _amendTweets.ToArray();
            set { _amendTweets = value.Guard().ToDictionary(kvp => kvp.Key, kvp => kvp.Value); }
        }

        public GeoLocationInfo AttachedGeoLocation
        {
            get => _geoInfo;
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

        [NotNull]
        public DispatcherCollection<byte[]> AttachedImages => _attachedImages;

        #endregion Accessing properties

        [NotNull]
        private InputData Clone()
        {
            var newdata = new InputData(_initText)
            {
                _accounts = _accounts?.ToArray(),
                _amendTweets = _amendTweets.ToDictionary(p => p.Key, p => p.Value),
                _boundTags = _boundTags,
                _geoInfo = _geoInfo,
                _inReplyTo = _inReplyTo,
                _initText = _initText,
                _messageRecipient = _messageRecipient,
                _text = _text
            };
            foreach (var image in _attachedImages)
            {
                newdata.AttachedImages.Add(image);
            }
            return newdata;
        }

        [CanBeNull]
        public async Task<PostResult> SendAsync()
        {
            var posts = _accounts.Guard()
                                 .Select(a => Tuple.Create(a, SendOnSingleAccount(a)))
                                 .ToArray();

            var amendTargets = new Dictionary<TwitterAccount, TwitterStatus>();
            var failedAccounts = new List<TwitterAccount>();
            var exceptions = new List<Exception>();

            foreach (var item in posts)
            {
                var account = item.Item1;
                try
                {
                    var result = await item.Item2.ConfigureAwait(false);
                    amendTargets.Add(account, result.Result);
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
                successData = Clone();
                successData.AmendTargetTweets = amendTargets.ToArray();
                successData.Accounts = amendTargets.Select(p => p.Key);
            }

            if (failedAccounts.Count > 0)
            {
                failedData = Clone();
                failedData.Accounts = failedAccounts.ToArray();
            }

            var pr = new PostResult(successData, failedData, exceptions.ToArray());


            if (IsAmend && AmendTargetTweets != null)
            {
                var amends = AmendTargetTweets
                    .Select(pair => RequestManager.Enqueue(
                        new DeleteStatusRequest(pair.Key.CreateAccessor(), pair.Value.Id,
                            pair.Value.StatusType)))
                    .OfType<Task>().ToArray();
                Task.WaitAll(amends);
            }
            return pr;
        }

        private async Task<IApiResult<TwitterStatus>> SendOnSingleAccount(TwitterAccount account)
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
            var accessor = account.CreateAccessor();
            if (IsDirectMessage && MessageRecipient != null)
            {
                if (MessageRecipient == null)
                {
                    throw new InvalidOperationException("recipient is not specified. please re-set target recipient.");
                }
                var result = await RequestManager.Enqueue(new SendMessageRequest(accessor,
                    new UserParameter(MessageRecipient.Id),
                    Text));
                return result;
            }
            if (_attachedImages.Count > 0)
            {
                // step 1: upload media
                var mac = account.CreateAccessor(EndpointType.UploadEndpoint);
                var mediaRequests = _attachedImages
                    .Select(b => new UploadMediaRequest(mac, b))
                    .Select(async r => await RequestManager.Enqueue(r).ConfigureAwait(false));
                var results = (await Task.WhenAll(mediaRequests)).Select(s => s.Result.MediaId).ToArray();
                // step 2: send tweet
                return await RequestManager.Enqueue(new TweetWithMediaRequest(accessor, Text + binds, results,
                    account.MarkMediaAsPossiblySensitive, InReplyTo?.Id, AttachedGeoLocation?.ToTuple()));
            }
            else
            {
                return await RequestManager.Enqueue(new TweetRequest(accessor, Text + binds, InReplyTo?.Id,
                    AttachedGeoLocation?.ToTuple()));
            }
        }
    }

    public class PostResult
    {
        public PostResult([CanBeNull] InputData succeededs, [CanBeNull] InputData faileds,
            [CanBeNull] IEnumerable<Exception> thrown)
        {
            Succeededs = succeededs;
            Faileds = faileds;
            Exceptions = thrown ?? throw new ArgumentNullException(nameof(thrown));
        }

        [CanBeNull]
        public InputData Succeededs { get; }

        [CanBeNull]
        public InputData Faileds { get; }

        [CanBeNull]
        public IEnumerable<Exception> Exceptions { get; }
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
                    { typeof(BmpBitmapDecoder), ImageType.Bmp },
                    { typeof(GifBitmapDecoder), ImageType.Gif },
                    { typeof(JpegBitmapDecoder), ImageType.Jpg },
                    { typeof(PngBitmapDecoder), ImageType.Png },
                    { typeof(TiffBitmapDecoder), ImageType.Tiff }
                };
                var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.None);
                return table.Where(imageType => decoder.GetType() == imageType.Key)
                            .Select(imageType => imageType.Value)
                            .FirstOrDefault();
            }
        }
    }
}