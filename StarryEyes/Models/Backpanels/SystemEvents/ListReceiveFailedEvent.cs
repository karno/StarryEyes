using StarryEyes.Models.Connections.Extends;

namespace StarryEyes.Models.Backpanels.SystemEvents
{
    public sealed class ListReceiveFailedEvent : SystemEventBase
    {
        private readonly ListInfo _info;

        public ListReceiveFailedEvent(ListInfo info)
        {
            _info = info;
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Warning; }
        }

        public override string Id
        {
            get { return "LIST_RECEIVER_ACCOUNT_NOT_FOUND_" + _info; }
        }

        public override string Detail
        {
            get { return "リスト " + _info + " を受信するアカウントが特定できません。(他人のリストを受信する際は、どのアカウントを用いるか明示的に指定する必要があります。)"; }
        }
    }
}
