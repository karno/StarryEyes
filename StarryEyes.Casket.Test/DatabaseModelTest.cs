using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket.DatabaseModels;
using StarryEyes.Casket.DatabaseModels.Generators;

namespace StarryEyes.Casket.Test
{
    /// <summary>
    /// DatabaseModelTest の概要の説明
    /// </summary>
    [TestClass]
    public class DatabaseModelTest
    {
        private DatabaseStatus _dbStatus;
        private DatabaseUser _dbUser;
        private DatabaseEntity _dbEntity;

        public DatabaseModelTest()
        {
            _dbStatus = new DatabaseStatus
            {
                CreatedAt = new DateTime(2013, 07, 29, 02, 05, 30),
                Id = 271828182845904L,
                InReplyToScreenName = "in_reply_to",
                InReplyToStatusId = 314159265358979L,
                InReplyToUserId = 14142135623730950L,
                Latitude = 1.7320508,
                Longitude = 2.23620679,
                Recipient = null,
                RetweetOriginalId = null,
                Source = "<a href=\"http://fabrikam.com/\">contoso</a>",
                StatusType = StatusType.Tweet,
                Text = "春はあけぼの。やうやう白くなりゆく山際、少しあかりて、紫だちたる雲の細くたなびきたる。",
                UserId = 114541919810893L
            };
            _dbUser = new DatabaseUser
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
            _dbEntity = new DatabaseEntity
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

        private string GetStat(DbModelBase model)
        {
            var str =
                "CREATOR: " + model.TableCreator + Environment.NewLine +
                "INSERTER: " + model.TableInserter + Environment.NewLine +
                "UPDATER: " + model.TableUpdator + Environment.NewLine +
                "DELETER: " + model.TableDeletor;
            System.Diagnostics.Debug.WriteLine(str);
            return str;
        }

        private const string StatusExpected = @"CREATOR: CREATE TABLE Status(Id INT PRIMARY KEY, StatusType INT NOT NULL, UserId INT NOT NULL, Text TEXT NOT NULL, CreatedAt DATETIME NOT NULL, Source TEXT, InReplyToStatusId INT, InReplyToUserId INT, InReplyToScreenName TEXT, Longitude REAL, Latitude REAL, RetweetOriginalId INT, Recipient INT, TableName TEXT NOT NULL, TableCreator TEXT NOT NULL, TableInserter TEXT NOT NULL, TableUpdator TEXT NOT NULL, TableDeletor TEXT NOT NULL);
INSERTER: INSERT INTO Status(Id, StatusType, UserId, Text, CreatedAt, Source, InReplyToStatusId, InReplyToUserId, InReplyToScreenName, Longitude, Latitude, RetweetOriginalId, Recipient, TableName, TableCreator, TableInserter, TableUpdator, TableDeletor) VALUES (@Id, @StatusType, @UserId, @Text, @CreatedAt, @Source, @InReplyToStatusId, @InReplyToUserId, @InReplyToScreenName, @Longitude, @Latitude, @RetweetOriginalId, @Recipient, @TableName, @TableCreator, @TableInserter, @TableUpdator, @TableDeletor);
UPDATER: UPDATE Status SET Id = @Id, StatusType = @StatusType, UserId = @UserId, Text = @Text, CreatedAt = @CreatedAt, Source = @Source, InReplyToStatusId = @InReplyToStatusId, InReplyToUserId = @InReplyToUserId, InReplyToScreenName = @InReplyToScreenName, Longitude = @Longitude, Latitude = @Latitude, RetweetOriginalId = @RetweetOriginalId, Recipient = @Recipient, TableName = @TableName, TableCreator = @TableCreator, TableInserter = @TableInserter, TableUpdator = @TableUpdator, TableDeletor = @TableDeletor WHERE Id = @Id;
DELETER: DELETE Status WHERE Id = @Id;";

        [TestMethod]
        public void TestStatus()
        {
            Assert.AreEqual(this.GetStat(_dbStatus), StatusExpected);
        }

        private const string UserExpected = @"CREATOR: CREATE TABLE User(Id INT PRIMARY KEY, ScreenName TEXT NOT NULL, Name TEXT NOT NULL, Description TEXT NOT NULL, Location TEXT NOT NULL, Url TEXT NOT NULL, IsDefaultProfileImage BOOLEAN NOT NULL, ProfileImageUri TEXT NOT NULL, ProfileBackgroundImageUri TEXT NOT NULL, ProfileBannerUri TEXT NOT NULL, IsProtected BOOLEAN NOT NULL, IsVerified BOOLEAN NOT NULL, IsTranslator BOOLEAN NOT NULL, IsContributorsEnabled BOOLEAN NOT NULL, IsGeoEnabled BOOLEAN NOT NULL, StatusesCount INT NOT NULL, FollowingCount INT NOT NULL, FollowersCount INT NOT NULL, FavoritesCount INT NOT NULL, ListedCount INT NOT NULL, Language TEXT NOT NULL, CreatedAt DATETIME NOT NULL, TableName TEXT NOT NULL, TableCreator TEXT NOT NULL, TableInserter TEXT NOT NULL, TableUpdator TEXT NOT NULL, TableDeletor TEXT NOT NULL);
INSERTER: INSERT INTO User(Id, ScreenName, Name, Description, Location, Url, IsDefaultProfileImage, ProfileImageUri, ProfileBackgroundImageUri, ProfileBannerUri, IsProtected, IsVerified, IsTranslator, IsContributorsEnabled, IsGeoEnabled, StatusesCount, FollowingCount, FollowersCount, FavoritesCount, ListedCount, Language, CreatedAt, TableName, TableCreator, TableInserter, TableUpdator, TableDeletor) VALUES (@Id, @ScreenName, @Name, @Description, @Location, @Url, @IsDefaultProfileImage, @ProfileImageUri, @ProfileBackgroundImageUri, @ProfileBannerUri, @IsProtected, @IsVerified, @IsTranslator, @IsContributorsEnabled, @IsGeoEnabled, @StatusesCount, @FollowingCount, @FollowersCount, @FavoritesCount, @ListedCount, @Language, @CreatedAt, @TableName, @TableCreator, @TableInserter, @TableUpdator, @TableDeletor);
UPDATER: UPDATE User SET Id = @Id, ScreenName = @ScreenName, Name = @Name, Description = @Description, Location = @Location, Url = @Url, IsDefaultProfileImage = @IsDefaultProfileImage, ProfileImageUri = @ProfileImageUri, ProfileBackgroundImageUri = @ProfileBackgroundImageUri, ProfileBannerUri = @ProfileBannerUri, IsProtected = @IsProtected, IsVerified = @IsVerified, IsTranslator = @IsTranslator, IsContributorsEnabled = @IsContributorsEnabled, IsGeoEnabled = @IsGeoEnabled, StatusesCount = @StatusesCount, FollowingCount = @FollowingCount, FollowersCount = @FollowersCount, FavoritesCount = @FavoritesCount, ListedCount = @ListedCount, Language = @Language, CreatedAt = @CreatedAt, TableName = @TableName, TableCreator = @TableCreator, TableInserter = @TableInserter, TableUpdator = @TableUpdator, TableDeletor = @TableDeletor WHERE Id = @Id;
DELETER: DELETE User WHERE Id = @Id;";

        [TestMethod]
        public void TestUser()
        {
            Assert.AreEqual(this.GetStat(_dbUser), UserExpected);
        }

        private const string EntityExpected = @"CREATOR: CREATE TABLE DatabaseEntity(Id INT PRIMARY KEY, ParentId INT NOT NULL, EntityType INT NOT NULL, DisplayText TEXT NOT NULL, OriginalText TEXT NOT NULL, MediaUrl TEXT, StartIndex INT NOT NULL, EndIndex INT NOT NULL, TableName TEXT NOT NULL, TableCreator TEXT NOT NULL, TableInserter TEXT NOT NULL, TableUpdator TEXT NOT NULL, TableDeletor TEXT NOT NULL);
INSERTER: INSERT INTO DatabaseEntity(Id, ParentId, EntityType, DisplayText, OriginalText, MediaUrl, StartIndex, EndIndex, TableName, TableCreator, TableInserter, TableUpdator, TableDeletor) VALUES (@Id, @ParentId, @EntityType, @DisplayText, @OriginalText, @MediaUrl, @StartIndex, @EndIndex, @TableName, @TableCreator, @TableInserter, @TableUpdator, @TableDeletor);
UPDATER: UPDATE DatabaseEntity SET Id = @Id, ParentId = @ParentId, EntityType = @EntityType, DisplayText = @DisplayText, OriginalText = @OriginalText, MediaUrl = @MediaUrl, StartIndex = @StartIndex, EndIndex = @EndIndex, TableName = @TableName, TableCreator = @TableCreator, TableInserter = @TableInserter, TableUpdator = @TableUpdator, TableDeletor = @TableDeletor WHERE Id = @Id;
DELETER: DELETE DatabaseEntity WHERE Id = @Id;";

        [TestMethod]
        public void TestEntity()
        {
            Assert.AreEqual(this.GetStat(_dbEntity), EntityExpected);
        }



    }
}
