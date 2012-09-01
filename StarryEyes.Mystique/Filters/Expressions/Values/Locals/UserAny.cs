using System.Collections.Generic;
using System.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.Mystique.Settings;

namespace StarryEyes.Mystique.Filters.Expressions.Values.Locals
{
    public sealed class UserAny : UserRepresentationBase
    {
        public override ICollection<long> Users
        {
            get
            {
                var accounts = new AVLTree<long>();
                Setting.Accounts.ForEach(a => accounts.Add(a.UserId));
                return new PseudoCollection<long>(id => accounts.Contains(id));
            }
        }

        public override ICollection<long> Followers
        {
            get
            {
                return new PseudoCollection<long>(id => AccountDataStore.GetAccountDatas()
                    .Any(_ => _.IsFollowedBy(id)));
            }
        }

        public override ICollection<long> Followings
        {
            get
            {
                return new PseudoCollection<long>(id => AccountDataStore.GetAccountDatas()
                    .Any(_ => _.IsFollowing(id)));
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
