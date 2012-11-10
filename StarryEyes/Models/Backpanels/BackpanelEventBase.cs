using System.Windows.Media;

namespace StarryEyes.Models.Backpanels
{
    /// <summary>
    /// Represents event.
    /// </summary>
    public abstract class BackpanelEventBase
    {
        /// <summary>
        /// Title of the event.
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// Detail description of the event.
        /// </summary>
        public abstract string Detail { get; }

        /// <summary>
        /// Background color of the event.
        /// </summary>
        public abstract Color Background { get; }

        /// <summary>
        /// Foreground color of the event.
        /// </summary>
        public virtual Color Foreground { get { return Colors.White; } }
    }
}
