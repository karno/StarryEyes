using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Values.Statuses
{
    internal static class StatusesUtil
    {
        public static TwitterStatus GetOriginal(this TwitterStatus status)
        {
            if (status.RetweetedOriginal != null)
                return status.RetweetedOriginal;
            else
                return status;
        }
    }
}
