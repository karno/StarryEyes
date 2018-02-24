using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AsyncOAuth;
using Cadena;
using Cadena.Data;
using JetBrains.Annotations;
using StarryEyes.Settings;

namespace StarryEyes.Models.Accounting
{
    /// <summary>
    /// Describe twitter authentication data.
    /// </summary>
    public sealed class TwitterAccount : IOAuthCredential, INotifyPropertyChanged
    {
        // accessed from serializer
        [UsedImplicitly]
        public TwitterAccount()
        {
            Id = 0;
            UnreliableScreenName = String.Empty;
            OAuthAccessToken = String.Empty;
            OAuthAccessTokenSecret = String.Empty;
        }

        public TwitterAccount(long id, string screenName, [CanBeNull] AccessToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            Id = id;
            UnreliableScreenName = screenName;
            OAuthAccessToken = token.Key;
            OAuthAccessTokenSecret = token.Secret;
            // default settings
            IsUserStreamsEnabled = true;
        }

        /// <summary>
        /// Id of the user.
        /// </summary>
        public long Id { get; set; }

        #region Authorization property

        /// <summary>
        /// User specified consumer key.
        /// </summary>
        [CanBeNull]
        public string OverridedConsumerKey { get; set; }

        /// <summary>
        /// User specified consumer secret.
        /// </summary>
        [CanBeNull]
        public string OverridedConsumerSecret { get; set; }

        /// <summary>
        /// Access token of this account.
        /// </summary>
        [CanBeNull]
        public string OAuthAccessToken { get; set; }

        /// <summary>
        /// Token secret of this account.
        /// </summary>
        [CanBeNull]
        public string OAuthAccessTokenSecret { get; set; }

        #endregion Authorization property

        #region Cache property

        /// <summary>
        /// Screen Name of user. This is a cache, so do not use this property for identifying user.
        /// </summary>
        [CanBeNull]
        public string UnreliableScreenName
        {
            get => _unreliableScreenName;
            set
            {
                if (_unreliableScreenName == value) return;
                _unreliableScreenName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Profile image of user. This is a cache property, so do not use this property for identifying user.
        /// </summary>
        [CanBeNull]
        public Uri UnreliableProfileImage
        {
            get => _unreliableProfileImage;
            set
            {
                if (_unreliableProfileImage == value) return;
                _unreliableProfileImage = value;
                OnPropertyChanged();
            }
        }

        [CanBeNull]
        public TwitterUser GetPseudoUser()
        {
            return new TwitterUser(Id, UnreliableScreenName, UnreliableProfileImage);
        }

        #endregion Cache property

        #region Volatile Property

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string OAuthConsumerKey => OverridedConsumerKey ??
                                          Setting.GlobalConsumerKey.Value ??
                                          App.ConsumerKey;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string OAuthConsumerSecret => OverridedConsumerSecret ??
                                             Setting.GlobalConsumerSecret.Value ??
                                             App.ConsumerSecret;

        private AccountRelationData _relationData;

        [CanBeNull]
        private string _unreliableScreenName;

        [CanBeNull]
        private Uri _unreliableProfileImage;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AccountRelationData RelationData => _relationData ?? (_relationData = new AccountRelationData(Id));

        #endregion Volatile Property

        #region Account configuration property

        /// <summary>
        /// Indicates user ID to fallback
        /// </summary>
        public long? FallbackAccountId { get; set; }

        /// <summary>
        /// Fallback favorites
        /// </summary>
        public bool IsFallbackFavorite { get; set; }

        /// <summary>
        /// Whether use user stream connection
        /// </summary>
        public bool IsUserStreamsEnabled { get; set; }

        /// <summary>
        /// Receive replies=all
        /// </summary>
        public bool ReceiveRepliesAll { get; set; }

        /// <summary>
        /// Mark uploaded medias as treat sensitive.<para/>
        /// This property value is inherited when fallbacking.
        /// </summary>
        public bool MarkMediaAsPossiblySensitive { get; set; }

        /// <summary>
        /// Receive all followings activities
        /// </summary>
        public bool ReceiveFollowingsActivity { get; set; }

        #endregion Account configuration property

        public ApiAccessor CreateAccessor(EndpointType endpoint = EndpointType.DefaultEndpoint, bool useGZip = true)
        {
            string E2N(string s) => String.IsNullOrEmpty(s) ? null : s;
            string eps;
            switch (endpoint)
            {
                case EndpointType.DefaultEndpoint:
                    eps = E2N(Setting.ApiProxy.Value) ?? ApiAccessor.DefaultEndpoint;
                    break;
                case EndpointType.StreamEndpoint:
                    eps = E2N(Setting.ApiProxyStreaming.Value) ?? ApiAccessor.DefaultEndpointForUserStreams;
                    break;
                case EndpointType.UploadEndpoint:
                    eps = E2N(Setting.ApiProxyUpload.Value) ?? ApiAccessor.DefaultEndpointForUpload;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null);
            }
            var ua = Setting.UserAgent.Value ?? ApiAccessor.DefaultUserAgent;
            return new ApiAccessor(this, eps, Setting.GetWebProxy(), ua, useGZip);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum EndpointType
    {
        DefaultEndpoint,
        StreamEndpoint,
        UploadEndpoint
    }
}