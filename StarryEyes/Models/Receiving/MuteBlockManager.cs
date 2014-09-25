using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using JetBrains.Annotations;
using StarryEyes.Albireo.Collections;
using StarryEyes.Albireo.Helpers;
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
        private static volatile bool _isOfficialMuteInvalidated = true;

        private static IDisposable _blockingDisposable;
        private static IDisposable _noRetweetDisposable;
        private static IDisposable _officialMuteDisposable;
        private static AVLTree<long> _noRetweetUserIds = new AVLTree<long>();
        private static AVLTree<long> _blockingUserIds = new AVLTree<long>();
        private static AVLTree<long> _officialMuteUserIds = new AVLTree<long>();
        private static Func<TwitterStatus, bool> _muteFilter = _ => false;
        private static bool _muteBlockedUsers = true;
        private static bool _muteNoRetweets = true;
        private static bool _muteOfficialMutes = true;
        private static string _muteSqlQuery = string.Empty;

        public static event Action RefreshTimelineRequired;

        /// <summary>
        /// Initializer method
        /// </summary>
        internal static void Initialize()
        {
            Setting.Accounts.Collection.ListenCollectionChanged(_ =>
            {
                InvalidateBlocks();
                InvalidateMute();
                InvalidateNoRetweets();
            });
            Setting.Muteds.ValueChanged += _ =>
            {
                InvalidateMute();
                System.Diagnostics.Debug.WriteLine("#INVALIDATION from mute word updated.");
                RefreshTimelineRequired.SafeInvoke();
            };
            Setting.MuteBlockingUsers.ValueChanged += _ =>
            {
                _muteBlockedUsers = Setting.MuteBlockingUsers.Value;
                System.Diagnostics.Debug.WriteLine("#INVALIDATION from mute user updated.");
                RefreshTimelineRequired.SafeInvoke();
            };
            Setting.MuteNoRetweets.ValueChanged += _ =>
            {
                _muteNoRetweets = Setting.MuteNoRetweets.Value;
                System.Diagnostics.Debug.WriteLine("#INVALIDATION from mute no retweets updated.");
                RefreshTimelineRequired.SafeInvoke();
            };
            Setting.MuteOfficialMutes.ValueChanged += _ =>
            {
                _muteOfficialMutes = Setting.MuteOfficialMutes.Value;
                System.Diagnostics.Debug.WriteLine("#INVALIDATION from official mute updated.");
                RefreshTimelineRequired.SafeInvoke();
            };
            // initialize value
            _muteBlockedUsers = Setting.MuteBlockingUsers.Value;
            _muteNoRetweets = Setting.MuteNoRetweets.Value;
            _muteOfficialMutes = Setting.MuteOfficialMutes.Value;
        }

        public static string FilteringSql
        {
            get
            {
                string exceptSql;
                var idFindTarget = new List<string>();
                var rtIdFindTarget = new List<string>();
                if (_muteBlockedUsers)
                {
                    idFindTarget.Add("Blockings");
                    rtIdFindTarget.Add("Blockings");
                }
                if (_muteOfficialMutes)
                {
                    idFindTarget.Add("Mutes");
                    rtIdFindTarget.Add("Mutes");
                }
                if (_muteNoRetweets)
                {
                    rtIdFindTarget.Add("NoRetweets");
                }
                if (idFindTarget.Count > 0 && rtIdFindTarget.Count > 0)
                {
                    var a = idFindTarget.Aggregate(String.Empty,
                        (c, t) => c.SqlConcatAnd("BaseUserId NOT IN (select TargetId from " + t + ")"));
                    var r = rtIdFindTarget.Aggregate(String.Empty,
                        (c, t) => c.SqlConcatAnd("RetweeterId NOT IN (select TargetId from " + t + ")"));
                    exceptSql = a.SqlConcatAnd("RetweeterId IS NULL".SqlConcatOr(r));
                }
                else if (rtIdFindTarget.Count > 0)
                {
                    var r = rtIdFindTarget.Aggregate(String.Empty,
                        (c, t) => c.SqlConcatAnd("RetweeterId NOT IN (select TargetId from " + t + ")"));
                    exceptSql = "RetweeterId IS NULL".SqlConcatOr(r);

                }
                else
                {
                    exceptSql = String.Empty;
                }
                CheckUpdateMutes();
                return Setting.UseLightweightMute.Value ? exceptSql : _muteSqlQuery.SqlConcatAnd(exceptSql);
            }
        }

        public static bool IsUnwanted([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            if (_muteBlockedUsers &&
                (IsBlocked(status.User) ||
                 (status.RetweetedOriginal != null && IsBlocked(status.RetweetedOriginal.User))))
            {
                // blocked user
                return true;
            }
            if (_muteOfficialMutes &&
                (IsOfficialMuted(status.User) ||
                 (status.RetweetedOriginal != null && IsOfficialMuted(status.RetweetedOriginal.User))))
            {
                // official muted user
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

        public static bool IsOfficialMuted([NotNull] TwitterUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            return IsOfficialMuted(user.Id);
        }

        public static bool IsOfficialMuted(long userId)
        {
            CheckUpdateOfficialMutes();
            lock (_officialMuteUserIds)
            {
                return _officialMuteUserIds.Contains(userId);
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

        private static void CheckUpdateOfficialMutes()
        {
            lock (_officialMuteUserIds)
            {
                if (!_isOfficialMuteInvalidated) return;
                _isOfficialMuteInvalidated = false;
                UpdateOfficialMutes();
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
                                 .Where(info => info.Type == RelationDataType.Blocking)
                                 .Subscribe(_ => InvalidateBlocks())))
                // select blocked users
                   .SelectMany(r => r.Blockings.Items)
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
                                 .Where(info => info.Type == RelationDataType.NoRetweets)
                                 .Subscribe(_ => InvalidateNoRetweets())))
                // select no retweet users
                   .SelectMany(r => r.NoRetweets.Items)
                   .ForEach(repl.Add);
            _noRetweetUserIds = repl;

            var prev = _noRetweetDisposable;
            _noRetweetDisposable = disposables;
            if (prev != null)
            {
                prev.Dispose();
            }
        }

        private static void UpdateOfficialMutes()
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
                                 .Where(info => info.Type == RelationDataType.Mutes)
                                 .Subscribe(_ => InvalidateOfficialMutes())))
                // select no retweet users
                   .SelectMany(r => r.Mutes.Items)
                   .ForEach(repl.Add);
            _officialMuteUserIds = repl;

            var prev = _officialMuteDisposable;
            _officialMuteDisposable = disposables;
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

        private static void InvalidateOfficialMutes()
        {
            _isOfficialMuteInvalidated = true;
        }
    }
}
