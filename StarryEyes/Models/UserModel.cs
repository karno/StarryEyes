using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models
{
    public class UserModel
    {
        public static UserModel Get(TwitterUser user)
        {
            return new UserModel(user);
        }

        private UserModel(TwitterUser user)
        {
            User = user;
        }

        public TwitterUser User { get; private set; }
    }
}
