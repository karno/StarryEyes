using System.Collections.Generic;
using System.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Models.Store;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class UserAny : UserRepresentationBase
    {
        public override IReadOnlyCollection<long> Users
        {
            get
            {
                return AccountsStore.AccountIds;
            }
        }

        public override IReadOnlyCollection<long> Followers
        {
            get
            {
                var followers = new AVLTree<long>();
                AccountsStore.Accounts
                    .SelectMany(a => AccountRelationDataStore.GetAccountData(a.UserId).Followings)
                    .ForEach(followers.Add);
                return followers;
            }
        }

        public override IReadOnlyCollection<long> Followings
        {
            get
            {
                var followings = new AVLTree<long>();
                AccountsStore.Accounts
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
