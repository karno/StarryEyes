using StarryEyes.Models.Accounting;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents.PostEvents
{
    public sealed class FallbackedEvent : BackstageEventBase
    {
        private readonly TwitterAccount _account;
        public TwitterAccount Account
        {
            get { return this._account; }
        }

        private readonly TwitterAccount _fallbackAccount;
        public TwitterAccount FallbackAccount
        {
            get { return this._fallbackAccount; }
        }


        public FallbackedEvent(TwitterAccount account, TwitterAccount fallbackTo)
        {
            this._account = account;
            this._fallbackAccount = fallbackTo;
        }

        public override string Title
        {
            get { return "FALLBACKED"; }
        }

        public override string Detail
        {
            get
            {
                return this._account.UnreliableScreenName +
                    " はPOST規制されているため、 " +
                    this._fallbackAccount.UnreliableScreenName +
                    " へフォールバックされました。";
            }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Purple; }
        }
    }
}
