using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Hubs;
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
                StartReceive(ai.AuthenticateInfo, info);
            else
                AppInformationHub.PublishInformation(new AppInformation(AppInformationKind.Warning,
                    "LIST_RECEIVER_NOT_FOUND_" + info,
                    "リストを受信するアカウントが検索できません。(対象リスト: " + info + ")",
                    "リストをどのアカウントで受信するのか分かりませんでした。" + Environment.NewLine +
                    "(他者のリストは受信アカウントを自動決定できません。明示的にどのアカウントから受信するか指定する必要があります。)" + Environment.NewLine +
                    "(アカウントの@IDを変更した場合は、リストの所属@IDも変更しなければいけません。)" + Environment.NewLine +
                    "最も手早い方法として、リスト受信しているタブの受信設定をやり直すのが良いと思います。"));
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
            DoReceive(AuthInfo, _receive);
        }

        public static void DoReceive(AuthenticateInfo info, ListInfo list, long? maxId = null)
        {
            info.GetListStatuses(slug: list.Slug, owner_screen_name: list.OwnerScreenName, max_id: maxId)
                .RegisterToStore();
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
