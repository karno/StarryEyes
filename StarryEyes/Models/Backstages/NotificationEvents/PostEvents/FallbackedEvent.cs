using System.Windows.Media;
using StarryEyes.Globalization;
using StarryEyes.Models.Accounting;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents.PostEvents
{
    public sealed class FallbackedEvent : BackstageEventBase
    {
        private readonly TwitterAccount _account;
        public TwitterAccount Account => _account;

        private readonly TwitterAccount _fallbackAccount;
        public TwitterAccount FallbackAccount => _fallbackAccount;

        public FallbackedEvent(TwitterAccount account, TwitterAccount fallbackTo)
        {
            _account = account;
            _fallbackAccount = fallbackTo;
        }

        public override string Title => "FALLBACKED";

        public override string Detail => BackstageResources.FallbackFormat.SafeFormat(
            "@" + _account.UnreliableScreenName,
            "@" + _fallbackAccount.UnreliableScreenName);

        public override Color Background => MetroColors.Violet;
    }
}