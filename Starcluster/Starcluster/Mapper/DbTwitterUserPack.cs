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
    public class DbTwitterUserPack
    {
        public static DbTwitterUserPack ToPack([NotNull] TwitterUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return new DbTwitterUserPack(user);
        }

        public DbTwitterUserPack([NotNull] TwitterUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            User = new DbTwitterUser(user);
            UserDescriptionMentionEntity =
                user.DescriptionEntities
                    .OfType<TwitterUserMentionEntity>()
                    .Select(e => new DbUserMentionEntity(user.Id, e))
                    .ToArray();
            UserDescriptionHashtagEntity =
                user.DescriptionEntities
                    .OfType<TwitterHashtagEntity>()
                    .Select(e => new DbTextEntity(user.Id, e))
                    .ToArray();
            UserDescriptionSymbolEntity =
                user.DescriptionEntities
                    .OfType<TwitterSymbolEntity>()
                    .Select(e => new DbTextEntity(user.Id, e))
                    .ToArray();
            UserDescriptionUrlEntity =
                user.DescriptionEntities
                    .OfType<TwitterUrlEntity>()
                    .Select(e => new DbUrlEntity(user.Id, e))
                    .ToArray();
            UserUrlEntity =
                user.UrlEntities
                    .Select(e => new DbUrlEntity(user.Id, e))
                    .ToArray();
        }

        public DbTwitterUserPack([NotNull] DbTwitterUser user,
            [NotNull] IEnumerable<DbUserMentionEntity> entDescMention,
            [NotNull] IEnumerable<DbTextEntity> entDescHashtag,
            [NotNull] IEnumerable<DbTextEntity> entDescSymbol,
            [NotNull] IEnumerable<DbUrlEntity> entDescUrl,
            [NotNull] IEnumerable<DbUrlEntity> entUrl)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            UserDescriptionMentionEntity = entDescMention ?? throw new ArgumentNullException(nameof(entDescMention));
            UserDescriptionHashtagEntity = entDescHashtag ?? throw new ArgumentNullException(nameof(entDescHashtag));
            UserDescriptionSymbolEntity = entDescSymbol ?? throw new ArgumentNullException(nameof(entDescSymbol));
            UserDescriptionUrlEntity = entDescUrl ?? throw new ArgumentNullException(nameof(entDescUrl));
            UserUrlEntity = entUrl ?? throw new ArgumentNullException(nameof(entUrl));
        }

        public TwitterUser ToTwitterUser()
        {
            return User.ToTwitterUser(
                UserUrlEntity.Select(e => e.ToUrlEntity()).ToArray(),
                UserDescriptionMentionEntity.Select(e => e.ToUserMentionEntity()).Concat<TwitterEntity>(
                                                UserDescriptionHashtagEntity.Select(e => e.ToHashtagEntity())).Concat(
                                                UserDescriptionSymbolEntity.Select(e => e.ToSymbolEntity())).Concat(
                                                UserDescriptionHashtagEntity.Select(e => e.ToHashtagEntity()))
                                            .ToArray());
        }

        public DbTwitterUser User { get; }

        public IEnumerable<DbUserMentionEntity> UserDescriptionMentionEntity { get; }

        public IEnumerable<DbTextEntity> UserDescriptionHashtagEntity { get; }

        public IEnumerable<DbTextEntity> UserDescriptionSymbolEntity { get; }

        public IEnumerable<DbUrlEntity> UserDescriptionUrlEntity { get; }

        public IEnumerable<DbUrlEntity> UserUrlEntity { get; }
    }
}