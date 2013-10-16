using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using StarryEyes.Albireo.Collections;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving
{
    public static class MuteBlockManager
    {
        private static volatile bool _isMuteInvalidated = true;
        private static volatile bool _isBlockInvalidated = true;

        private static IDisposable _blockingDisposable = null;
        private static AVLTree<long> _blockingUserIds = new AVLTree<long>();
        private static Func<TwitterStatus, bool> _muteFilter = _ => false;
        private static string _muteSqlQuery = string.Empty;

        internal static void Initialize()
        {
            Setting.Accounts.Collection.ListenCollectionChanged().Subscribe(_ =>
            {
                InvalidateBlockingUsers();
                InvalidateMute();
            });
            Setting.Muteds.ValueChanged += _ => InvalidateMute();
        }

        public static bool IsBlockedOrMuted([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            if (IsBlocked(status.User)) return true;
            CheckUpdateMutes();
            return _muteFilter(status);
        }

        public static bool IsMuted(TwitterStatus status)
        {
            return _muteFilter(status);
        }

        public static bool IsBlocked([NotNull] TwitterUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            return IsBlocked(user.Id);
        }

        public static bool IsBlocked(long userId)
        {
            CheckUpdateBlocks();
            lock (_blockingUserIds)
            {
                return _blockingUserIds.Contains(userId);
            }
        }

        public static string FilteringSql
        {
            get
            {
                CheckUpdateMutes();
                CheckUpdateBlocks();
                string blocksql;
                lock (_blockingUserIds)
                {
                    blocksql = _blockingUserIds.Select(s => s.ToString()).JoinString(",");
                }
                if (!String.IsNullOrEmpty(blocksql))
                {
                    blocksql = "UserId NOT IN (" + blocksql + ")";
                }
                return _muteSqlQuery.SqlConcatAnd(blocksql);
            }
        }

        private static void CheckUpdateMutes()
        {
            lock (_muteFilter)
            {
                if (!_isMuteInvalidated) return;
                _isMuteInvalidated = false;
                UpdateMute();
            }
        }

        private static void CheckUpdateBlocks()
        {
            lock (_blockingUserIds)
            {
                if (!_isBlockInvalidated) return;
                _isBlockInvalidated = false;
                UpdateBlockingUsers();
            }
        }

        private static void UpdateBlockingUsers()
        {
            var disposables = new CompositeDisposable();
            var nbs = new AVLTree<long>();
            Setting.Accounts
                   .Collection
                   .Select(a => a.RelationData)
                   .Do(r => disposables.Add(
                       Observable.FromEvent<RelationDataChangedInfo>(
                           h => r.AccountDataUpdated += h,
                           h => r.AccountDataUpdated -= h)
                                 .Where(info => info.Change == RelationDataChange.Blocking)
                                 .Subscribe(_ => InvalidateBlockingUsers())))
                   .SelectMany(r => r.Blockings)
                   .ForEach(nbs.Add);
            _blockingUserIds = nbs;

            var prev = _blockingDisposable;
            _blockingDisposable = disposables;
            if (prev != null)
            {
                prev.Dispose();
            }
        }

        private static void UpdateMute()
        {
            var eval = Setting.Muteds.Evaluator;
            var sql = Setting.Muteds.Value.GetSqlQuery();
            var accIds = Setting.Accounts.Ids
                                .Select(i => i.ToString(CultureInfo.InvariantCulture))
                                .JoinString(",");
            _muteFilter = s => !Setting.Accounts.Contains(s.User.Id) && eval(s);
            _muteSqlQuery = String.IsNullOrEmpty(sql)
                                ? string.Empty
                                : "(UserId IN (" + accIds + ") OR NOT (" + sql + "))";
        }

        private static void InvalidateBlockingUsers()
        {
            _isBlockInvalidated = true;
        }

        private static void InvalidateMute()
        {
            _isMuteInvalidated = true;
        }
    }
}
