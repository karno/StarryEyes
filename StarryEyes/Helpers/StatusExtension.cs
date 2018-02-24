using Cadena.Data;

namespace StarryEyes.Helpers
{
    public static class StatusExtension
    {
        public static TwitterStatus GetOriginal(this TwitterStatus status)
        {
            return status.RetweetedStatus ?? status;
        }
    }
}