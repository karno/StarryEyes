using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Mystique.Models.Common;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.Mystique.Settings;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values.BuiltIns
{
    public abstract class UserRepresentationBase
    {
        public abstract ICollection<long> Followings { get; }

        public abstract ICollection<long> Followers { get; }

        public abstract string ToQuery();
    }

    public sealed class UserAny : UserRepresentationBase
    {
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

    public sealed class UserSpecified : UserRepresentationBase
    {
        long _userId;
        AccountData _adata;

        public UserSpecified(string screenName)
        {
            _userId = Setting.Accounts.Value
                .Where(u => u.AuthenticateInfo.UnreliableScreenName == screenName)
                .Select(u => u.UserId)
                .FirstOrDefault();
            GetAccountData();
        }

        public UserSpecified(long id)
        {
            _userId = id;
            GetAccountData();
        }

        private void GetAccountData()
        {
            _adata = AccountDataStore.GetAccountData(_userId);
        }

        public override ICollection<long> Followings
        {
            get
            {
                if (_adata == null)
                    return new List<long>(); // returns empty list
                else
                    return new PseudoCollection<long>(_ => _adata.IsFollowing(_));
            }
        }

        public override ICollection<long> Followers
        {
            get
            {
                if (_adata == null)
                    return new List<long>();
                else
                    return new PseudoCollection<long>(_ => _adata.IsFollowedBy(_));
            }
        }

        public override string ToQuery()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class UserFollowings : ValueBase
    {
        private UserRepresentationBase _representation;
        public UserFollowings(UserRepresentationBase representation)
        {
            this._representation = representation;
        }

        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Set }; }
        }

        public override ICollection<long> GetSetValue(TwitterStatus @unused)
        {
            return _representation.Followings;
        }

        public override string ToQuery()
        {
            return _representation.ToQuery() + ".followings"; // "friends" also ok
        }
    }

    public sealed class UserFollowers : ValueBase
    {
        private UserRepresentationBase _representation;
        public UserFollowers(UserRepresentationBase representation)
        {
            this._representation = representation;
        }

        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Set }; }
        }

        public override ICollection<long> GetSetValue(TwitterStatus @unused)
        {
            return _representation.Followers;
        }

        public override string ToQuery()
        {
            return _representation.ToQuery() + ".followers";
        }
    }

    class PseudoCollection<T> : ICollection<T>
    {
        Func<T, bool> containsFunc;
        public PseudoCollection(Func<T, bool> containsFunc)
        {
            this.containsFunc = containsFunc;
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            return containsFunc(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
