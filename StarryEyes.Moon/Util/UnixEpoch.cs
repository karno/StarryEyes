using System;

namespace StarryEyes.Moon.Util
{
    public static class UnixEpoch
    {
        const long EPOCH = 621355968000000000;

        /// <summary>
        /// Get Unix epoch from DateTime
        /// </summary>
        /// <param name="dt">DateTime</param>
        /// <returns>Unix epoch</returns>
        public static int GetUnixEpochByDateTime(DateTime dt)
        {
            return (int)((dt.ToUniversalTime().Ticks - EPOCH) / 10000000);
        }

        /// <summary>
        /// Get DateTime from Unix epoch
        /// </summary>
        /// <param name="epoch">Unix epoch</param>
        /// <returns>DateTime</returns>
        public static DateTime GetDateTimeByUnixEpoch(long epoch)
        {
            DateTime unix = new DateTime(1970, 1, 1);
            unix = unix.AddSeconds(epoch);
            return unix.ToLocalTime();
        }
    }
}
