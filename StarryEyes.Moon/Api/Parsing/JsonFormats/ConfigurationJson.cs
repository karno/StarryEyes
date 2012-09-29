using System.Collections.Generic;

namespace StarryEyes.Moon.Api.Parsing.JsonFormats
{
    public class ConfigurationJson
    {
        public int characters_reserved_per_media { get; set; }

        public string[] non_user_paths { get; set; }

        public int max_media_per_upload { get; set; }

        public int photo_size_limit { get; set; }

        public Dictionary<string, PhotoSizeInfoJson> photo_sizes { get; set; }

        public int short_url_length { get; set; }

        public int short_url_length_https { get; set; }
    }

    public class PhotoSizeInfoJson
    {
        public int w { get; set; }

        public int h { get; set; }

        public string resize { get; set; }
    }
}
