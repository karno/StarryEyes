using System;

namespace StarryEyes.Casket.DataModels
{
    public class DbList
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public string Name { get; set; }

        public string Slug { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsPrivate { get; set; }

        public string MemberIdsCsv { get; set; }
    }
}
