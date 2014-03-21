using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Receivers
{
    public sealed class ListMemberReceiver : CyclicReceiverBase
    {
        public event Action ListMemberChanged;

        private readonly ListInfo _listInfo;
        private readonly TwitterAccount _auth;
        private long? _listId;

        public ListMemberReceiver(TwitterAccount auth, ListInfo listInfo)
        {
            this._auth = auth;
            this._listInfo = listInfo;
        }

        protected override string ReceiverName
        {
            get
            {
                return "リスト情報(" + this._listInfo + ")";
            }
        }

        protected override int IntervalSec
        {
            get { return Setting.ListMemberReceivePeriod.Value; }
        }

        protected override async Task DoReceive()
        {
            if (_listId == null)
            {
                // get description
                var list = (await ReceiveListDescription(_auth, _listInfo));
                await ListProxy.SetListDescription(list);
                _listId = list.Id;
            }
            // if list data is not found, abort receiving timeline.
            if (_listId == null) return;
            var id = _listId.Value;
            var users = (await ReceiveListMembers(_auth, id)).OrderBy(l => l).ToArray();
            var oldUsers = (await ListProxy.GetListMembers(id)).OrderBy(l => l).ToArray();
            if (users.SequenceEqual(oldUsers))
            {
                // not changed
                return;
            }
            // commit changes
            await ListProxy.SetListMembers(id, users);
            ListMemberChanged.SafeInvoke();
        }

        private async Task<TwitterList> ReceiveListDescription(TwitterAccount account, ListInfo info)
        {
            return await account.ShowListAsync(info.OwnerScreenName, info.Slug);
        }

        private async Task<IEnumerable<long>> ReceiveListMembers(TwitterAccount account, long listId)
        {
            var memberList = new List<long>();
            long cursor = -1;
            do
            {
                var result = await account.GetListMembersAsync(listId, cursor);
                memberList.AddRange(result.Result
                          .Do(u => Task.Run(() => UserProxy.StoreUserAsync(u)))
                          .Select(u => u.Id));
                cursor = result.NextCursor;
            } while (cursor != 0);
            return memberList;
        }
    }
}
