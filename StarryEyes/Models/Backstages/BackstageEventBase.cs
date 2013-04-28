using System.Windows.Media;

namespace StarryEyes.Models.Backstages
{
    /// <summary>
    /// Represents event.
    /// </summary>
    public abstract class BackstageEventBase
    {
        /// <summary>
        /// Title of the event.
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// Image title of the event.<para />
        /// If not available, returns null value.
        /// </summary>
        public virtual ImageSource TitleImage { get { return null; } }

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
