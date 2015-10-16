using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
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
            _auth = auth;
            _listInfo = listInfo;
        }

        protected override string ReceiverName
        {
            get { return ReceivingResources.ReceiverListInfoFormat.SafeFormat(_listInfo); }
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
                var list = await ReceiveListDescription(_auth, _listInfo).ConfigureAwait(false);
                await ListProxy.SetListDescription(list).ConfigureAwait(false);
                _listId = list.Id;
            }
            // if list data is not found, abort receiving timeline.
            if (_listId == null) return;
            var id = _listId.Value;
            var users = (await ReceiveListMembers(_auth, id).ConfigureAwait(false)).OrderBy(l => l).ToArray();
            var oldUsers = (await ListProxy.GetListMembers(id).ConfigureAwait(false)).OrderBy(l => l).ToArray();
            if (users.SequenceEqual(oldUsers))
            {
                // not changed
                return;
            }
            // commit changes
            await ListProxy.SetListMembers(id, users).ConfigureAwait(false);
            ListMemberChanged.SafeInvoke();
        }

        private Task<TwitterList> ReceiveListDescription(TwitterAccount account, ListInfo info)
        {
            return account.ShowListAsync(info.OwnerScreenName, info.Slug);
        }

        private async Task<IEnumerable<long>> ReceiveListMembers(TwitterAccount account, long listId)
        {
            var memberList = new List<long>();
            long cursor = -1;
            do
            {
                var result = await account.GetListMembersAsync(listId, cursor).ConfigureAwait(false);
                memberList.AddRange(result.Result
                          .Do(u => Task.Run(() => UserProxy.StoreUser(u)))
                          .Select(u => u.Id));
                cursor = result.NextCursor;
            } while (cursor != 0);
            return memberList;
        }
    }
}
