using System;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Connection.Polling
{
    public class ListReceiver : PollingConnectionBase
    {
        private ListInfo _receive;
        public ListReceiver(AuthenticateInfo ainfo, ListInfo linfo) : base(ainfo)
        {
            _receive = linfo;
        }

        protected override int IntervalSec
        {
            get { return 60; }
        }

        protected override void DoReceive()
        {
            AuthInfo.GetListStatuses(slug: _receive.Slug, owner_screen_name: _receive.OwnerScreenName)
                .Subscribe(t => StatusStore.Store(t));
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
            return this.OwnerScreenName == this.OwnerScreenName && other.Slug == this.Slug;
        }

        public override int GetHashCode()
        {
            return OwnerScreenName.GetHashCode() ^ Slug.GetHashCode();
        }
    }
}
