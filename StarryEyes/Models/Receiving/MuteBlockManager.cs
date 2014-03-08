using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using StarryEyes.Albireo;
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
        private static volatile bool _isNoRetweetsInvalidated = true;

        private static IDisposable _blockingDisposable;
        private static IDisposable _noRetweetDisposable;
        private static AVLTree<long> _noRetweetUserIds = new AVLTree<long>();
        private static AVLTree<long> _blockingUserIds = new AVLTree<long>();
        private static Func<TwitterStatus, bool> _muteFilter = _ => false;
        private static bool _muteBlockedUsers = true;
        private static bool _muteNoRetweets = true;
        private static string _muteSqlQuery = string.Empty;

        public static event Action RefreshTimelineRequired;

        /// <summary>
        /// Initializer method
        /// </summary>
        internal static void Initialize()
        {
            Setting.Accounts.Collection.ListenCollectionChanged().Subscribe(_ =>
            {
                InvalidateBlocks();
                InvalidateMute();
                InvalidateNoRetweets();
            });
            Setting.Muteds.ValueChanged += _ =>
            {
                InvalidateMute();
                RefreshTimelineRequired.SafeInvoke();
            };
            Setting.MuteBlockedUsers.ValueChanged += _ =>
            {
                _muteBlockedUsers = Setting.MuteBlockedUsers.Value;
                RefreshTimelineRequired.SafeInvoke();
            };
            Setting.MuteNoRetweets.ValueChanged += _ =>
            {
                _muteNoRetweets = Setting.MuteNoRetweets.Value;
                RefreshTimelineRequired.SafeInvoke();
            };
        }

        public static string FilteringSql
        {
            get
            {
                string exceptSql;
                if (_muteBlockedUsers && _muteNoRetweets)
                {
                    exceptSql =
                        "BaseUserId NOT IN (select TargetId from Blockings) AND " +
                        "(RetweeterId IS NULL OR " +
                        "(RetweeterId NOT IN (select TargetId from NoRetweets) AND " +
                        "RetweeterId NOT IN (select TargetId from Blockings)))";
                }
                else if (_muteBlockedUsers)
                {
                    // mute blocked users
                    exceptSql =
                        "BaseUserId NOT IN (select TargetId from Blockings) AND " +
                        "(RetweeterId IS NULL OR RetweeterId NOT IN (select TargetId from Blockings))";
                }
                else
                {
                    // mute no-retweet users
                    exceptSql =
                        "RetweeterId IS NULL OR RetweeterId NOT IN (select TargetId from NoRetweets)";
                }
                CheckUpdateMutes();
                return _muteSqlQuery.SqlConcatAnd(exceptSql);
            }
        }

        public static bool IsUnwanted([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            if (_muteBlockedUsers && (IsBlocked(status.User) ||
                 (status.RetweetedOriginal != null && IsBlocked(status.RetweetedOriginal.User))))
            {
                // blocked user
                return true;
            }
            if (status.RetweetedOriginal != null &&
                _muteNoRetweets && IsNoRetweet(status.User))
            {
                // no retweet specified
                return true;
            }
            return IsMuted(status);
        }

        public static bool IsMuted([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            CheckUpdateMutes();
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

        public static bool IsNoRetweet([NotNull] TwitterUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            return IsNoRetweet(user.Id);
        }

        public static bool IsNoRetweet(long userId)
        {
            CheckUpdateNoRetweets();
            lock (_noRetweetUserIds)
            {
                return _noRetweetUserIds.Contains(userId);
            }
        }

        private static void CheckUpdateMutes()
        {
            lock (_muteFilter)
            {
                if (!_isMuteInvalidated) return;
                _isMuteInvalidated = false;
                UpdateMutes();
            }
        }

        private static void CheckUpdateBlocks()
        {
            lock (_blockingUserIds)
            {
                if (!_isBlockInvalidated) return;
                _isBlockInvalidated = false;
                UpdateBlocks();
            }
        }

        private static void CheckUpdateNoRetweets()
        {
            lock (_noRetweetUserIds)
            {
                if (!_isNoRetweetsInvalidated) return;
                _isNoRetweetsInvalidated = false;
                UpdateNoRetweets();
            }
        }

        private static void UpdateMutes()
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

        private static void UpdateBlocks()
        {
            var disposables = new CompositeDisposable();
            var repl = new AVLTree<long>();
            Setting.Accounts
                   .Collection
                   .Select(a => a.RelationData)
                // listen block change info
                   .Do(r => disposables.Add(
                       Observable.FromEvent<RelationDataChangedInfo>(
                           h => r.AccountDataUpdated += h,
                           h => r.AccountDataUpdated -= h)
                                 .Where(info => info.Change == RelationDataChange.Blocking)
                                 .Subscribe(_ => InvalidateBlocks())))
                // select blocked users
                   .SelectMany(r => r.Blockings)
                   .ForEach(repl.Add);
            _blockingUserIds = repl;

            var prev = _blockingDisposable;
            _blockingDisposable = disposables;
            if (prev != null)
            {
                prev.Dispose();
            }
        }

        private static void UpdateNoRetweets()
        {
            var disposables = new CompositeDisposable();
            var repl = new AVLTree<long>();
            Setting.Accounts
                   .Collection
                   .Select(a => a.RelationData)
                // listen no retweet change info
                   .Do(r => disposables.Add(
                       Observable.FromEvent<RelationDataChangedInfo>(
                           h => r.AccountDataUpdated += h,
                           h => r.AccountDataUpdated -= h)
                                 .Where(info => info.Change == RelationDataChange.NoRetweets)
                                 .Subscribe(_ => InvalidateNoRetweets())))
                // select blocked users
                   .SelectMany(r => r.NoRetweets)
                   .ForEach(repl.Add);
            _noRetweetUserIds = repl;

            var prev = _noRetweetDisposable;
            _noRetweetDisposable = disposables;
            if (prev != null)
            {
                prev.Dispose();
            }
        }

        private static void InvalidateMute()
        {
            _isMuteInvalidated = true;
        }

        private static void InvalidateBlocks()
        {
            _isBlockInvalidated = true;
        }

        private static void InvalidateNoRetweets()
        {
            _isNoRetweetsInvalidated = true;
        }
    }
}
