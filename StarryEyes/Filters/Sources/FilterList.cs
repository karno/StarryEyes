using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Albireo.Collections;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Receiving.Receivers;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Sources
{
    public class FilterList : FilterSourceBase
    {
        #region Receive list member and update database
        #endregion

        #region Filtering

        #endregion
        private readonly string _receiver;
        private readonly ListInfo _listInfo;
        private readonly AVLTree<long> _ids = new AVLTree<long>();
        private bool _isPreparing = false;
        public override bool IsPreparing
        {
            get { return _isPreparing; }
        }

        public FilterList(string ownerAndslug)
        {
            var splited = ownerAndslug.Split('/');
            if (splited.Length < 2 || splited.Length > 3)
            {
                throw new ArgumentException("owner and slug must be separated as slash, once.");
            }
            if (splited.Length == 2)
            {
                _listInfo = new ListInfo { OwnerScreenName = splited[0], Slug = splited[1] };
                _receiver = splited[0];
            }
            else
            {
                _listInfo = new ListInfo { OwnerScreenName = splited[1], Slug = splited[2] };
                _receiver = splited[0];
            }
        }

        private void GetListUsersInfo(bool enforceReceive = false)
        {
            _isPreparing = true;
            System.Diagnostics.Debug.WriteLine("receiving list: " + this.FilterValue);
            Task.Run(async () =>
            {
                bool refreshRequired = false;
                try
                {
                    IEnumerable<long> users;
                    if (!enforceReceive && (users = CacheStore.GetListUsers(_listInfo)).Any())
                    {
                        lock (_ids)
                        {
                            if (users.Select(_ids.AddDistinct).ToArray().Any(b => b))
                            {
                                // diff exist
                                refreshRequired = true;
                            }
                        }
                        return;
                    }
                    var account = this.GetAccount();
                    if (account == null) return;
                    var memberList = new List<long>();
                    long cursor = -1;
                    do
                    {
                        var result = await account.GetListMembersAsync(
                            _listInfo.Slug, _listInfo.OwnerScreenName, cursor);
                        memberList.AddRange(result.Result
                                                  .Do(u => Task.Run(() => UserProxy.StoreUserAsync(u)))
                                                  .Select(u => u.Id));
                        cursor = result.NextCursor;
                    } while (cursor != 0);
                    if (memberList.Count <= 0) return;
                    lock (this._ids)
                    {
                        if (this._ids.OrderBy(_ => _).SequenceEqual(memberList.OrderBy(_ => _)))
                        {
                            // no diff
                            return;
                        }
                    }
                    refreshRequired = true;
                    CacheStore.SetListUsers(this._listInfo, memberList);
                    lock (this._ids)
                    {
                        this._ids.Clear();
                        memberList.ForEach(this._ids.Add);
                        System.Diagnostics.Debug.WriteLine("updated list: " + this.FilterValue + " users: " + _ids.Count);
                    }
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent("リスト情報を読み込めません(" + this.FilterValue + ")", ex));
                }
                finally
                {
                    _isPreparing = false;
                    if (refreshRequired)
                    {
                        this.RaiseInvalidateRequired();
                    }
                }
            });
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return s =>
            {
                lock (_ids)
                {
                    return _ids.Contains(s.User.Id);
                }
            };
        }

        public override string GetSqlQuery()
        {
            string ida;
            lock (_ids)
            {
                ida = this._ids.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",");
            }
            return "UserId in (" + ida + ")";
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            return ListReceiver.DoReceive(this.GetAccount(), this._listInfo, maxId);
        }

        private TwitterAccount GetAccount()
        {
            return Setting.Accounts.Collection
                          .FirstOrDefault(
                              a => this._receiver.Equals(a.UnreliableScreenName,
                                                         StringComparison.CurrentCultureIgnoreCase)) ??
                   Setting.Accounts.GetRandomOne();
        }

        public override string FilterKey
        {
            get { return "list"; }
        }

        public override string FilterValue
        {
            get
            {
                return this._receiver == this._listInfo.OwnerScreenName
                           ? this._listInfo.OwnerScreenName + "/" + this._listInfo.Slug
                           : this._receiver + "/" + this._listInfo.OwnerScreenName + "/" + this._listInfo.Slug;
            }
        }

        private Timer _timer;
        private bool _isActivated;

        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            if (!String.IsNullOrEmpty(_receiver))
            {
                ReceiveManager.RegisterList(_receiver, _listInfo);
            }
            else
            {
                ReceiveManager.RegisterList(_listInfo);
            }
            Task.Factory.StartNew(() => this.GetListUsersInfo());
            _timer = new Timer(_ => this.TimerCallback(), null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(30));
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            ReceiveManager.UnregisterList(_listInfo);
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void TimerCallback()
        {
            if (!_isActivated)
            {
                _timer.Dispose();
                _timer = null;
                return;
            }
            this.GetListUsersInfo(true);
        }
    }
}
