using System;
using System.Linq;
using Cadena.Data;
using Cadena.Data.Entities;
using JetBrains.Annotations;
using Starcluster.Infrastructures;
using Starcluster.Mapper;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Starcluster.Models
{
    [DbName("Status")]
    public class DbTwitterStatus
    {
        public DbTwitterStatus()
        {
            // set default values to non-null properties
            Text = String.Empty;
            BigramText = String.Empty;
        }

        public DbTwitterStatus([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            Id = status.Id;
            StatusType = status.StatusType;
            UserId = status.User.Id;
            BaseUserId = (status.RetweetedStatus ?? status).User.Id;
            Text = status.Text;
            DisplayTextRangeBegin = status.DisplayTextRange?.Item1;
            DisplayTextRangeEnd = status.DisplayTextRange?.Item2;
            CreatedAt = status.CreatedAt;
            Source = status.Source;
            InReplyToStatusId = status.InReplyToStatusId;
            InReplyToUserId = status.InReplyToUserId;
            InReplyToScreenName = status.InReplyToScreenName;
            Latitude = status.Coordinates?.Item1;
            Longitude = status.Coordinates?.Item2;
            RetweetedStatusId = status.RetweetedStatus?.Id;
            QuotedStatusId = status.QuotedStatus?.Id;
            RecipientId = status.Recipient?.Id;

            // db properties
            ContainsPhoto = status.Entities.OfType<TwitterMediaEntity>()
                                  .Any(m => m.MediaType == MediaType.Photo);
            ContainsVideo = status.Entities.OfType<TwitterMediaEntity>()
                                  .Any(m => m.MediaType == MediaType.Video ||
                                            m.MediaType == MediaType.AnimatedGif);
            ContainsMedia = ContainsPhoto || ContainsVideo;
            InReplyToOrRecipientId = RecipientId ?? InReplyToStatusId;
            InReplyToOrRecipientScreenName = status.Recipient?.ScreenName ?? status.InReplyToScreenName;

            // make bigram text
            BigramText = BigramTextGenerator.Generate(status.Text);
        }

        public TwitterStatus ToTwitterStatus([NotNull] TwitterUser user, [NotNull] TwitterEntity[] entities,
            [CanBeNull] TwitterStatus retweeted = null, [CanBeNull] TwitterStatus quoted = null,
            [CanBeNull] TwitterUser recipient = null)
        {
            // check parameters
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (user.Id != UserId)
            {
                throw new ArgumentException($"id of {nameof(user)} and {nameof(UserId)} is not matched", nameof(user));
            }
            DbModelHelper.AssertCorrelation(retweeted, RetweetedStatusId, nameof(retweeted), nameof(RetweetedStatusId));
            DbModelHelper.AssertCorrelation(quoted, QuotedStatusId, nameof(quoted), nameof(QuotedStatusId));
            DbModelHelper.AssertCorrelation(recipient, RecipientId, nameof(recipient), nameof(RecipientId));

            switch (StatusType)
            {
                case StatusType.Tweet:
                    return new TwitterStatus(Id, user, Text,
                        DbModelHelper.CreateNullableTuple(DisplayTextRangeBegin, DisplayTextRangeEnd),
                        CreatedAt, entities, Source, InReplyToStatusId, InReplyToUserId,
                        null, null, InReplyToScreenName,
                        DbModelHelper.CreateNullableTuple(Latitude, Longitude), retweeted, quoted);

                case StatusType.DirectMessage:
                    // redundant assertion
                    if (recipient == null) throw new ArgumentNullException(nameof(recipient));
                    return new TwitterStatus(Id, user, recipient, Text,
                        DbModelHelper.CreateNullableTuple(DisplayTextRangeBegin, DisplayTextRangeEnd),
                        CreatedAt, entities);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [DbPrimaryKey]
        public long Id { get; set; }

        public StatusType StatusType { get; set; }

        public long UserId { get; set; }

        public long BaseUserId { get; set; }

        [NotNull]
        public string Text { get; set; }

        public int? DisplayTextRangeBegin { get; set; }

        public int? DisplayTextRangeEnd { get; set; }

        public DateTime CreatedAt { get; set; }

        #region Properties for statuses

        [CanBeNull, DbOptional]
        public string Source { get; set; }

        public long? InReplyToStatusId { get; set; }

        public long? InReplyToUserId { get; set; }

        [CanBeNull, DbOptional]
        public string InReplyToScreenName { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public long? RetweetedStatusId { get; set; }

        public long? QuotedStatusId { get; set; }

        #endregion Properties for statuses

        #region Properties for messages

        public long? RecipientId { get; set; }

        #endregion Properties for messages

        #region Properties for database query

        public bool ContainsMedia { get; set; }

        public bool ContainsPhoto { get; set; }

        public bool ContainsVideo { get; set; }

        public long? InReplyToOrRecipientId { get; set; }

        [CanBeNull, DbOptional]
        public string InReplyToOrRecipientScreenName { get; set; }

        [NotNull]
        public string BigramText { get; set; }

        #endregion Properties for database query
    }
}