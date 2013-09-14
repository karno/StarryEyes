using System;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Test
{
    public class TestModelProvider
    {
        private readonly DatabaseStatus _dbStatus;
        private readonly DatabaseUser _dbUser;
        private readonly DatabaseEntity _dbEntity;

        public TestModelProvider()
        {
            this._dbStatus = new DatabaseStatus
            {
                CreatedAt = new DateTime(2013, 07, 29, 02, 05, 30),
                Id = 271828182845904L,
                InReplyToScreenName = "in_reply_to",
                InReplyToStatusId = 314159265358979L,
                InReplyToUserId = 14142135623730950L,
                Latitude = 1.7320508,
                Longitude = 2.23620679,
                RecipientId = null,
                RetweetOriginalId = null,
                Source = "<a href=\"http://fabrikam.com/\">contoso</a>",
                StatusType = StatusType.Tweet,
                Text = "春はあけぼの。やうやう白くなりゆく山際、少しあかりて、紫だちたる雲の細くたなびきたる。",
                UserId = 114541919810893L
            };
            this._dbUser = new DatabaseUser
            {

                CreatedAt = new DateTime(2013, 07, 29, 02, 05, 30),
                Id = 271828182845904L,
                Description = "夏は夕暮れ。月の頃はさらなり。闇もなほ、蛍のおほく飛びちがひたる。",
                FavoritesCount = 1024,
                FollowersCount = 2048,
                FollowingCount = 4096,
                IsContributorsEnabled = false,
                IsDefaultProfileImage = false,
                IsGeoEnabled = false,
                IsProtected = false,
                IsTranslator = false,
                IsVerified = false,
                Language = "ja-JP",
                ListedCount = 8192,
                Location = "圏外",
                Name = "はる",
                ProfileBackgroundImageUri = "https://www.google.co.jp/images/srpr/logo3w.png",
                ProfileBannerUri = "https://www.google.co.jp/images/srpr/logo4w.png",
                ProfileImageUri = "https://www.google.co.jp/images/srpr/logo1w.png",
                ScreenName = "haru067"
            };
            this._dbEntity = new DatabaseStatusEntity
            {
                DisplayText = "http://fabrikam.com/",
                EndIndex = 48,
                EntityType = EntityType.Urls,
                Id = 271828182845904L,
                MediaUrl = "http://fabrikam.com/media",
                OriginalText = "http://fabrikam.com/original",
                ParentId = 1054571172647L,
                StartIndex = 24,
            };
        }


        public DatabaseStatus DbStatus
        {
            get { return this._dbStatus; }
        }

        public DatabaseUser DbUser
        {
            get { return this._dbUser; }
        }

        public DatabaseEntity DbEntity
        {
            get { return this._dbEntity; }
        }
    }
}
