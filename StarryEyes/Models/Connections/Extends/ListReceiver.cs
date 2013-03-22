using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Connections.Extends
{
    public sealed class ListReceiver : PollingConnectionBase
    {
        private static readonly object ListReceiverLocker = new object();
        private static readonly SortedDictionary<ListInfo, ListReceiver> ListReceiverResolver
            = new SortedDictionary<ListInfo, ListReceiver>();
        private static readonly SortedDictionary<ListInfo, int> ListReceiverReferenceCount
            = new SortedDictionary<ListInfo, int>();

        public static void StartReceive(ListInfo info)
        {
            var ai = AccountsStore.Accounts.FirstOrDefault(aset => aset.AuthenticateInfo.UnreliableScreenName == info.OwnerScreenName);
            if (ai != null)
            {
                StartReceive(ai.AuthenticateInfo, info);
            }
            else
            {
                BackpanelModel.RegisterEvent(new ListReceiveFailedEvent(info));
            }
        }

        public static void StartReceive(string receiverScreenName, ListInfo info)
        {
            var ai = AccountsStore.Accounts.FirstOrDefault(aset => aset.AuthenticateInfo.UnreliableScreenName == receiverScreenName);
            if (ai != null)
                StartReceive(ai.AuthenticateInfo, info);
            else
                StartReceive(info);
        }

        public static void StartReceive(AuthenticateInfo auth, ListInfo info)
        {
            lock (ListReceiverLocker)
            {
                if (ListReceiverReferenceCount.ContainsKey(info))
                {
                    ListReceiverReferenceCount[info]++;
                }
                else
                {
                    var lr = new ListReceiver(auth, info) { IsActivated = true };
                    ListReceiverReferenceCount.Add(info, 1);
                    ListReceiverResolver.Add(info, lr);
                }
            }
        }

        public static void StopReceive(ListInfo info)
        {
            lock (ListReceiverLocker)
            {
                if (!ListReceiverReferenceCount.ContainsKey(info))
                    return;
                if (--ListReceiverReferenceCount[info] == 0)
                {
                    // dispose connection
                    ListReceiverReferenceCount.Remove(info);
                    var lr = ListReceiverResolver[info];
                    ListReceiverResolver.Remove(info);
                    lr.IsActivated = false;
                    lr.Dispose();
                }
            }
        }

        private readonly ListInfo _receive;
        private ListReceiver(AuthenticateInfo ainfo, ListInfo linfo)
            : base(ainfo)
        {
            _receive = linfo;
        }

        protected override int IntervalSec
        {
            get { return 60; }
        }

        protected override void DoReceive()
        {
            DoReceive(AuthInfo, _receive).RegisterToStore();
        }

        public static IObservable<TwitterStatus> DoReceive(AuthenticateInfo info, ListInfo list, long? maxId = null)
        {
            return info.GetListStatuses(slug: list.Slug, owner_screen_name: list.OwnerScreenName, max_id: maxId);
        }
    }

    public class ListInfo : IEquatable<ListInfo>
    {
        public string Slug { get; set; }

        public string OwnerScreenName { get; set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ListInfo);
        }

        public bool Equals(ListInfo other)
        {
            if (other == null) return false;
            return other.OwnerScreenName == this.OwnerScreenName && other.Slug == this.Slug;
        }

        public override int GetHashCode()
        {
            return OwnerScreenName.GetHashCode() ^ Slug.GetHashCode();
        }

        public override string ToString()
        {
            return OwnerScreenName + "/" + Slug;
        }
    }
}
