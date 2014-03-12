using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Databases;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Receivers
{
    public sealed class ListMemberReceiver : CyclicReceiverBase
    {
        public event Action ListMemberChanged;

        private readonly ListInfo _listInfo;
        private readonly TwitterAccount _auth;

        public ListMemberReceiver(TwitterAccount auth, ListInfo listInfo)
        {
            this._auth = auth;
            this._listInfo = listInfo;
        }

        protected override string ReceiverName
        {
            get
            {
                return "リスト メンバー(" +
                    (_auth == null ? "アカウント未指定" : "@" + _auth.UnreliableScreenName) + " - " +
                       this._listInfo;
            }
        }

        protected override int IntervalSec
        {
            get { return Setting.ListMemberReceivePeriod.Value; }
        }

        protected override void DoReceive()
        {
            Task.Run(async () =>
            {
                try
                {
                    var listData = await ReceiveListDescription(_auth, _listInfo);
                    var users = (await ReceiveListMembers(_auth, _listInfo)).OrderBy(l => l).ToArray();
                    var oldUsers = (await ListProxy.GetListMembers(listData.Id)).OrderBy(l => l).ToArray();
                    if (users.SequenceEqual(oldUsers))
                    {
                        // not changed
                        return;
                    }
                    // commit changes
                    await ListProxy.SetListMembers(listData, users);
                    ListMemberChanged.SafeInvoke();
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent(
                        "リスト情報を受信できませんでした: " + _auth.UnreliableScreenName + " -> " + _listInfo.ToString(),
                        ex));
                }
            });
        }

        private async Task<TwitterList> ReceiveListDescription(TwitterAccount account, ListInfo info)
        {
            return await account.ShowListAsync(info.OwnerScreenName, info.Slug);
        }

        private async Task<IEnumerable<long>> ReceiveListMembers(TwitterAccount account, ListInfo info)
        {
            var memberList = new List<long>();
            long cursor = -1;
            do
            {
                var result = await account.GetListMembersAsync(
                    _listInfo.Slug, _listInfo.OwnerScreenName, cursor);
                memberList.AddRange(
                    result.Result
                          .Do(u => Task.Run(() => UserProxy.StoreUserAsync(u)))
                          .Select(u => u.Id));
                cursor = result.NextCursor;
            } while (cursor != 0);
            return memberList;
        }
    }
}
