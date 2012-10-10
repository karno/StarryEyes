using System;
using System.Collections.Generic;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;
using StarryEyes.Filters.Expressions;

namespace StarryEyes.Models
{
    public static class MainWindowModel
    {
        public static void ExecuteAccountSelectAction(AccountSelectionAction action,
            IEnumerable<AuthenticateInfo> defaultSelected, Action<IEnumerable<AuthenticateInfo>> after)
        {
        }

        public static void ShowUserInfo(TwitterUser user)
        {
        }

        public static void ConfirmMute(string description, FilterExpressionBase addExpr)
        {
        }
    }

    public enum AccountSelectionAction
    {
        Favorite,
        Retweet,
    }
}
