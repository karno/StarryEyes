using System;
using System.Collections.Generic;
using System.Linq;
using Cadena.Data;
using Cadena.Data.Entities;
using JetBrains.Annotations;
using Starcluster.Models;
using Starcluster.Models.Entities;

namespace Starcluster.Mapper
{
    public class DbTwitterStatusPack
    {
        public static DbTwitterStatusPack ToPack([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            return new DbTwitterStatusPack(status);
        }

        public DbTwitterStatusPack([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            Status = new DbTwitterStatus(status);
            User = new DbTwitterUserPack(status.User);

            RetweetedStatus = status.RetweetedStatus != null
                ? new DbTwitterStatusPack(status.RetweetedStatus)
                : null;
            QuotedStatus = status.QuotedStatus != null
                ? new DbTwitterStatusPack(status.QuotedStatus)
                : null;

            MediaEntity =
                status.Entities
                      .OfType<TwitterMediaEntity>()
                      .Select(e => new DbMediaEntity(status.Id, e))
                      .ToArray();
            UserMentionEntity =
                status.Entities
                      .OfType<TwitterUserMentionEntity>()
                      .Select(e => new DbUserMentionEntity(status.Id, e))
                      .ToArray();
            HashtagEntity =
                status.Entities
                      .OfType<TwitterHashtagEntity>()
                      .Select(e => new DbTextEntity(status.Id, e))
                      .ToArray();
            SymbolEntity =
                status.Entities
                      .OfType<TwitterSymbolEntity>()
                      .Select(e => new DbTextEntity(status.Id, e))
                      .ToArray();
            UrlEntity =
                status.Entities
                      .OfType<TwitterUrlEntity>()
                      .Select(e => new DbUrlEntity(status.Id, e))
                      .ToArray();
        }

        public DbTwitterStatusPack(
            [NotNull] DbTwitterStatus status, [NotNull] DbTwitterUserPack userpack,
            [NotNull] IEnumerable<DbMediaEntity> entMedia,
            [NotNull] IEnumerable<DbUserMentionEntity> entMention,
            [NotNull] IEnumerable<DbTextEntity> entHashtag,
            [NotNull] IEnumerable<DbTextEntity> entSymbol,
            [NotNull] IEnumerable<DbUrlEntity> entUrl,
            [CanBeNull] DbTwitterStatusPack retweet,
            [CanBeNull] DbTwitterStatusPack quote)
        {
            Status = status ?? throw new ArgumentNullException(nameof(status));
            User = userpack ?? throw new ArgumentNullException(nameof(userpack));
            MediaEntity = entMedia ?? throw new ArgumentNullException(nameof(entMedia));
            UserMentionEntity = entMention ?? throw new ArgumentNullException(nameof(entMention));
            HashtagEntity = entHashtag ?? throw new ArgumentNullException(nameof(entHashtag));
            SymbolEntity = entSymbol ?? throw new ArgumentNullException(nameof(entSymbol));
            UrlEntity = entUrl ?? throw new ArgumentNullException(nameof(entUrl));
            RetweetedStatus = retweet;
            QuotedStatus = quote;
        }

        public TwitterStatus ToTwitterStatus()
        {
            return Status.ToTwitterStatus(
                User.ToTwitterUser(),
                MediaEntity.Select(e => e.ToMediaEntity()).Concat<TwitterEntity>(
                    UserMentionEntity.Select(e => e.ToUserMentionEntity())).Concat(
                    HashtagEntity.Select(e => e.ToHashtagEntity())).Concat(
                    SymbolEntity.Select(e => e.ToSymbolEntity())).Concat(
                    UrlEntity.Select(e => e.ToUrlEntity())).ToArray(),
                RetweetedStatus.ToTwitterStatus(), QuotedStatus.ToTwitterStatus());
        }

        public DbTwitterStatus Status { get; }

        public DbTwitterUserPack User { get; }

        public DbTwitterStatusPack RetweetedStatus { get; }

        public DbTwitterStatusPack QuotedStatus { get; }

        public IEnumerable<DbMediaEntity> MediaEntity { get; }

        public IEnumerable<DbUserMentionEntity> UserMentionEntity { get; }

        public IEnumerable<DbTextEntity> HashtagEntity { get; }

        public IEnumerable<DbTextEntity> SymbolEntity { get; }

        public IEnumerable<DbUrlEntity> UrlEntity { get; }
    }
}