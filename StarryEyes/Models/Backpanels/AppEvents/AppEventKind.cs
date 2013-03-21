
namespace StarryEyes.Models.Backpanels.AppEvents
{
    public enum AppEventKind
    {        /// <summary>
        /// User action is not required.
        /// </summary>
        Notify,
        /// <summary>
        /// Error raised, user action may be required, but this information does not know how to fix it.
        /// </summary>
        Warning,
        /// <summary>
        /// Error raised, user action required.
        /// </summary>
        Error,
    }
}
