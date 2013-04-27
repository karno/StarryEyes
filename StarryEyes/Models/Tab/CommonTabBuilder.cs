
namespace StarryEyes.Models.Tab
{
    public static class CommonTabBuilder
    {
        public static readonly string Empty = "from all where ()";

        private static TabModel SetDefaultParams(this TabModel model)
        {
            model.IsShowUnreadCounts = true;
            model.IsNotifyNewArrivals = true;
            return model;
        }

        public static TabModel GenerateGeneralTab()
        {
            return new TabModel("general", "from all where ()").SetDefaultParams();
        }

        public static TabModel GenerateHomeTab()
        {
            return new TabModel("home", "from local where user <- *.following | (retweet & (retweeter <- * | retweeter <- *.followings)) | user <- * | to -> *").SetDefaultParams();
        }

        public static TabModel GenerateMentionTab()
        {
            return new TabModel("mentions", "from all where to -> * & !retweet").SetDefaultParams();
        }

        public static TabModel GenerateMeTab()
        {
            return new TabModel("me", "from all where (user <- * & !retweet) | retweeter <- *").SetDefaultParams();
        }

        public static TabModel GenerateActivitiesTab()
        {
            return new TabModel("activities", "from all where user <- * & (favs > 0 | rts > 0)").SetDefaultParams();
        }
    }
}
