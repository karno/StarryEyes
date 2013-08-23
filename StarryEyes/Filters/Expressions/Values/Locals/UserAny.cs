using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Albireo.Data;
using StarryEyes.Models.Accounting;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Expressions.Values.Locals
{
    public sealed class UserAny : UserExpressionBase
    {
        private CompositeDisposable _disposables = new CompositeDisposable();

        public override IReadOnlyCollection<long> Users
        {
            get
            {
                return new AVLTree<long>(Setting.Accounts.Ids);
            }
        }

        public override IReadOnlyCollection<long> Followers
        {
            get
            {
                var followers = new AVLTree<long>(
                    Setting.Accounts
                           .Collection
                           .SelectMany(a => a.RelationData.Followers));
                return followers;
            }
        }

        public override IReadOnlyCollection<long> Following
        {
            get
            {
                var following = new AVLTree<long>(
                    Setting.Accounts
                           .Collection
                           .SelectMany(a => a.RelationData.Following));
                return following;
            }
        }

        public override IReadOnlyCollection<long> Blockings
        {
            get
            {
                var blockings = new AVLTree<long>(
                    Setting.Accounts
                           .Collection
                           .SelectMany(a => a.RelationData.Blockings));
                return blockings;
            }
        }

        public override string ToQuery()
        {
            return "our";
        }

        public override long UserId
        {
            get { return -1; } // an representive user is not existed.
        }

        public override void BeginLifecycle()
        {
            _disposables.Add(
                Setting.Accounts.Collection
                       .ListenCollectionChanged()
                       .Subscribe(_ => this.RaiseReapplyFilter(null)));
            _disposables.Add(
                Observable.FromEvent<RelationDataChangedInfo>(
                    h => AccountRelationData.AccountDataUpdatedStatic += h,
                    h => AccountRelationData.AccountDataUpdatedStatic -= h)
                          .Subscribe(this.RaiseReapplyFilter));
        }

        public override void EndLifecycle()
        {
            Interlocked.Exchange(ref _disposables, new CompositeDisposable()).Dispose();
        }
    }
}
