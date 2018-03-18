using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Albireo.Collections;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Models.Accounting;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class UserAny : UserExpressionBase
    {
        private CompositeDisposable _disposables = new CompositeDisposable();

        public override long UserId => -1;

        public override IReadOnlyCollection<long> Users => new AVLTree<long>(Setting.Accounts.Ids);

        public override IReadOnlyCollection<long> Followings
        {
            get
            {
                var following = new AVLTree<long>(
                    Setting.Accounts
                           .Collection
                           .SelectMany(a => a.RelationData.Followings.Items));
                return following;
            }
        }

        public override IReadOnlyCollection<long> Followers
        {
            get
            {
                var followers = new AVLTree<long>(
                    Setting.Accounts
                           .Collection
                           .SelectMany(a => a.RelationData.Followers.Items));
                return followers;
            }
        }

        public override IReadOnlyCollection<long> Blockings
        {
            get
            {
                var blockings = new AVLTree<long>(
                    Setting.Accounts
                           .Collection
                           .SelectMany(a => a.RelationData.Blockings.Items));
                return blockings;
            }
        }

        public override IReadOnlyCollection<long> Mutes
        {
            get
            {
                var mutes = new AVLTree<long>(
                    Setting.Accounts
                           .Collection
                           .SelectMany(a => a.RelationData.Mutes.Items));
                return mutes;
            }
        }

        public override string UserIdSql => "-1";

        public override string UsersSql => "(select Id from Accounts)";

        public override string FollowingsSql => "(select TargetId from Followings)";

        public override string FollowersSql => "(select TargetId from Followers)";

        public override string BlockingsSql => "(select TargetId from Blockings)";

        public override string MutesSql => "(select TargetId from Mutes)";

        public override string ToQuery()
        {
            return "our";
        }

        public override void BeginLifecycle()
        {
            _disposables.Add(Setting.Accounts.Collection.ListenCollectionChanged(
                _ => RaiseReapplyFilter(null)));
            _disposables.Add(
                Observable.FromEvent<RelationDataChangedInfo>(
                              h => AccountRelationData.AccountDataUpdatedStatic += h,
                              h => AccountRelationData.AccountDataUpdatedStatic -= h)
                          .Subscribe(RaiseReapplyFilter));
        }

        public override void EndLifecycle()
        {
            Interlocked.Exchange(ref _disposables, new CompositeDisposable()).Dispose();
        }
    }
}