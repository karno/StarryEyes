using System;
using System.ComponentModel;
using Codeplex.OAuth;
using StarryEyes.Annotations;
using StarryEyes.Anomaly;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Settings;

namespace StarryEyes.Models.Accounting
{
    /// <summary>
    /// Describe twitter authentication data.
    /// </summary>
    public sealed class TwitterAccount : IOAuthCredential
    {
        public TwitterAccount() { }

        public TwitterAccount(long id, string screenName, [NotNull] AccessToken token)
        {
            if (token == null) throw new ArgumentNullException("token");
            this.Id = id;
            this.UnreliableScreenName = screenName;
            this.OAuthAccessToken = token.Key;
            this.OAuthAccessTokenSecret = token.Secret;
            // default settings
            this.IsUserStreamsEnabled = true;
        }

        /// <summary>
        /// Id of the user.
        /// </summary>
        public long Id { get; set; }

        #region Authorization property

        /// <summary>
        /// User specified consumer key.
        /// </summary>
        public string OverridedConsumerKey { get; set; }

        /// <summary>
        /// User specified consumer secret.
        /// </summary>
        public string OverridedConsumerSecret { get; set; }

        /// <summary>
        /// Access token of this account.
        /// </summary>
        public string OAuthAccessToken { get; set; }

        /// <summary>
        /// Token secret of this account.
        /// </summary>
        public string OAuthAccessTokenSecret { get; set; }

        #endregion

        #region Cache property

        /// <summary>
        /// Screen Name of user. This is a cache, so do not use this property for identifying user.
        /// </summary>
        public string UnreliableScreenName { get; set; }

        /// <summary>
        /// Profile image of user. This is a cache property, so do not use this property for identifying user.
        /// </summary>
        public Uri UnreliableProfileImage { get; set; }

        [NotNull]
        public TwitterUser GetPserudoUser()
        {
            return new TwitterUser
            {
                ScreenName = UnreliableScreenName,
                ProfileImageUri = UnreliableProfileImage,
                Id = Id
            };
        }

        #endregion

        #region Volatile Property

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string OAuthConsumerKey
        {
            get
            {
                return this.OverridedConsumerKey ??
                       Setting.GlobalConsumerKey.Value ??
                       App.ConsumerKey;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string OAuthConsumerSecret
        {
            get
            {
                return this.OverridedConsumerSecret ??
                       Setting.GlobalConsumerSecret.Value ??
                       App.ConsumerSecret;
            }
        }

        private AccountRelationData _relationData;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AccountRelationData RelationData
        {
            get { return this._relationData ?? (this._relationData = new AccountRelationData(this.Id)); }
        }

        #endregion

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
        public bool IsReceiveRepliesAll { get; set; }

        /// <summary>
        /// Mark uploaded medias as treat sensitive.<para/>
        /// This property value is inherited when fallbacking.
        /// </summary>
        public bool IsMarkMediaAsPossiblySensitive { get; set; }

        #endregion
    }
}
