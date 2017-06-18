using System;
using System.Linq;
using Cadena.Data.Entities;
using Cadena.Meteor;
using JetBrains.Annotations;
using Starcluster.Infrastructures;

namespace Starcluster.Models.Entities
{
    public class DbMediaEntity : DbTwitterEntity
    {
        public DbMediaEntity()
        {
            MediaSizesJson = "{}";
        }

        public DbMediaEntity(long parentId, TwitterMediaEntity entity) : base(parentId, entity)
        {
            MediaId = entity.Id;
            MediaUrl = entity.MediaUrl;
            MediaUrlHttps = entity.MediaUrlHttps;
            Url = entity.Url;
            DisplayUrl = entity.DisplayUrl;
            ExpandedUrl = entity.ExpandedUrl;
            MediaSizesJson = DbModelHelper.DictionaryToJson(entity.MediaSizes, MediaSizesToJson);
            MediaType = entity.MediaType;
            VideoInfoJson = entity.VideoInfo == null ? null : VideoInfoToJson(entity.VideoInfo);
        }

        public TwitterMediaEntity ToMediaEntity()
        {
            var mediaSizes = DbModelHelper.JsonToDictionary(MediaSizesJson, jv => new MediaSize(jv));
            var videoInfo = VideoInfoJson == null ? null : new VideoInfo(MeteorJson.Parse(VideoInfoJson));
            return new TwitterMediaEntity(ToIndices(), MediaId,
                MediaUrl, MediaUrlHttps, MediaUrl, DisplayUrl, ExpandedUrl,
                MediaType, mediaSizes, videoInfo);
        }

        public string MediaSizesToJson(MediaSize size)
        {
            return "{" +
                   $"\"w\":{size.Width}," +
                   $"\"h\":{size.Height}," +
                   $"\"resize\":{size.ResizeString}," +
                   "}";
        }

        public string VideoInfoToJson(VideoInfo info)
        {
            var variants = String.Join(",", info.Variants.Select(VideoVariantsToJson));
            return "{" +
                   $"\"aspect_ratio\":[{info.AspectRatio.Item1},{info.AspectRatio.Item2}]," +
                   $"\"duration_millis\":{info.DurationMillis}," +
                   $"\"variants\":[{variants}]" +
                   "}";
        }

        public string VideoVariantsToJson(VideoVariant variant)
        {
            return "{" +
                   $"\"bitrate\":{variant.BitRate}," +
                   $"\"content_type\":{variant.ContentType}," +
                   $"\"url\":{variant.Url}" +
                   "}";
        }

        public long MediaId { get; set; }

        [CanBeNull, DbOptional]
        public string MediaUrl { get; set; }

        [CanBeNull, DbOptional]
        public string MediaUrlHttps { get; set; }

        [CanBeNull, DbOptional]
        public string Url { get; set; }

        [CanBeNull, DbOptional]
        public string DisplayUrl { get; set; }

        [CanBeNull, DbOptional]
        public string ExpandedUrl { get; set; }

        [NotNull]
        public string MediaSizesJson { get; set; }

        public MediaType MediaType { get; }

        [CanBeNull, DbOptional]
        public string VideoInfoJson { get; set; }
    }
}