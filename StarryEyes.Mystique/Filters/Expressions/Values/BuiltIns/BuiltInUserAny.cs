using System.Collections.Generic;
using System.Linq;
using StarryEyes.Albireo.Data;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.Mystique.Settings;

namespace StarryEyes.Mystique.Filters.Expressions.Values.BuiltIns
{
    public sealed class UserAny : UserRepresentationBase
    {
        public override ICollection<long> User
        {
            get
            {
                var accounts = new AVLTree<long>();
                Setting.Accounts.Value.ForEach(a => accounts.Add(a.UserId));
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
    }
}
