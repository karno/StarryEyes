using System.Threading.Tasks;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Requests
{
    /// <summary>
    /// Wrap request of Twitter API.
    /// </summary>
    /// <typeparam name="T">type of result object</typeparam>
    public abstract class RequestBase<T>
    {
        /// <summary>
        /// Request retry count. If you set this param as 0, request won't retry.
        /// </summary>
        public virtual int RetryCount { get { return 2; } }

        /// <summary>
        /// Retry wait time(sec)
        /// </summary>
        public virtual double RetryDelaySec { get { return 0.5; } }

        /// <summary>
        /// Send request.
        /// </summary>
        /// <returns></returns>
        public abstract Task<T> Send(TwitterAccount account);
    }
}
