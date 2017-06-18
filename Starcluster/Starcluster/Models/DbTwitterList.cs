using System;
using Cadena.Data;
using JetBrains.Annotations;
using Starcluster.Infrastructures;

namespace Starcluster.Models
{
    public class DbTwitterList
    {
        public DbTwitterList()
        {
            Name = String.Empty;
            FullName = String.Empty;
            Url = String.Empty;
            Slug = String.Empty;
            Description = String.Empty;
        }

        public DbTwitterList(TwitterList list)
        {
            Id = list.Id;
            UserId = list.User.Id;
            Name = list.Name;
            FullName = list.FullName;
            Url = list.Uri.ToString();
            Slug = list.Slug;
            ListMode = list.ListMode;
            Description = list.Description;
            MemberCount = list.MemberCount;
            SubscriberCount = list.SubscriberCount;
            CreatedAt = list.CreatedAt;
        }

        public TwitterList ToTwitterList(TwitterUser owner)
        {
            DbModelHelper.AssertCorrelation(owner, UserId, nameof(owner), nameof(UserId));
            return new TwitterList(Id, owner, Name, FullName,
                new Uri(Url, UriKind.Absolute), Slug, ListMode,
                Description, MemberCount, SubscriberCount, CreatedAt);
        }

        [DbPrimaryKey]
        public long Id { get; set; }

        public long UserId { get; set; }

        [NotNull]
        public string Name { get; set; }

        [NotNull]
        public string FullName { get; set; }

        [NotNull]
        public string Url { get; set; }

        [NotNull]
        public string Slug { get; set; }

        public ListMode ListMode { get; set; }

        [NotNull]
        public string Description { get; set; }

        public long MemberCount { get; set; }

        public long SubscriberCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}