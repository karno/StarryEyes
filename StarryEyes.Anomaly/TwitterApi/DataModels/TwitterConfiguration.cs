
namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public class TwitterConfiguration
    {
        public TwitterConfiguration() { }

        public TwitterConfiguration(dynamic json)
        {
            this.CharactersReservedPerMedia = 0; // media url -> 0 (extended_tweet)
            this.PhotoSizeLimit = (int)json.photo_size_limit;
            this.ShortUrlLength = (int)json.short_url_length;
            this.ShortUrlLengthHttps = (int)json.short_url_length_https;
        }

        public int CharactersReservedPerMedia { get; set; }

        // this property is not used in StarryEyes.
        // public string NonUserPaths { get; set; }

        // this property is not used in StarryEyes.
        // public int MaxMediaPerUpload { get; set; }

        public int PhotoSizeLimit { get; set; }

        public int ShortUrlLength { get; set; }

        public int ShortUrlLengthHttps { get; set; }
    }
}
