
namespace StarryEyes.Models.Timelines.Tabs
{
    public static class CommonTabBuilder
    {
        public static TabModel GenerateGeneralTab()
        {
            return TabModel.Create("general", "from all where ()");
        }

        public static TabModel GenerateHomeTab()
        {
            return TabModel.Create("home", "from local where user <- *.following | (retweet & (retweeter <- * | retweeter <- *.followings)) | user <- * | to -> *");
        }

        public static TabModel GenerateMentionTab()
        {
            return TabModel.Create("mentions", "from all where to -> * & !retweet");
        }

        public static TabModel GenerateMeTab()
        {
            return TabModel.Create("me", "from all where (user <- * & !retweet) | retweeter <- *");
        }

        public static TabModel GenerateActivitiesTab()
        {
            return TabModel.Create("activities", "from all where user <- * & (favs > 0 | rts > 0)");
        }
    }
}
