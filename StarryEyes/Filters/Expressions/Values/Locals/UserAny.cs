using System.Collections.Generic;
using System.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Models.Store;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class UserAny : UserRepresentationBase
    {
        public override ICollection<long> Users
        {
            get
            {
                var accounts = new AVLTree<long>();
                Setting.Accounts.ForEach(a => accounts.Add(a.UserId));
                return accounts;
            }
        }

        public override ICollection<long> Followers
        {
            get
            {
                var followers = new AVLTree<long>();
                Setting.Accounts
                    .SelectMany(a => AccountRelationDataStore.GetAccountData(a.UserId).Followings)
                    .ForEach(followers.Add);
                return followers;
            }
        }

        public override ICollection<long> Followings
        {
            get
            {
                var followings = new AVLTree<long>();
                Setting.Accounts
                    .SelectMany(a => AccountRelationDataStore.GetAccountData(a.UserId).Followings)
                    .ForEach(followings.Add);
                return followings;
            }
        }

        public override string ToQuery()
        {
            return "*";
        }

        public override long UserId
        {
            get { return 0; } // an representive user is not existed.
        }
    }
}
