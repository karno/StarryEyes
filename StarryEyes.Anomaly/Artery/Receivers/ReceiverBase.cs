
using System.Threading.Tasks;

namespace StarryEyes.Anomaly.Artery.Receivers
{
    /// <summary>
    /// Base class for requesting.
    /// </summary>
    public abstract class ReceiverBase
    {
        /// <summary>
        /// Execute request
        /// </summary>
        /// <param name="manager">assigned receive manager</param>
        /// <returns>task object for awaiting completion</returns>
        public abstract Task Execute(ReceiveManager manager);

        /// <summary>
        /// Get priority of this request
        /// </summary>
        public abstract RequestPriority Priority { get; }
    }

    public enum RequestPriority
    {
        /// <summary>
        /// Requested from user operation(blocking) or other urgent source
        /// </summary>
        High,
        /// <summary>
        /// Requested from user operation(non-blocking) or other middle priority source
        /// </summary>
        Middle,
        /// <summary>
        /// Requested from cyclic receiving or other postponable source
        /// </summary>
        Low
    }
}
