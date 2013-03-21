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

        public virtual string Id { get { return string.Empty; } }

        public virtual bool IsActionable { get { return false; } }

        public virtual void Action()
        {
        }

        public virtual EventRegistingKind RegisterKind { get { return EventRegistingKind.None; } }
    }

    public enum EventRegistingKind
    {
        /// <summary>
        /// No registering
        /// </summary>
        None,
        /// <summary>
        /// Exclusive store with ID
        /// </summary>
        IdExclusive,
        /// <summary>
        /// Use twitter queue mode
        /// </summary>
        TwitterQueue,
        /// <summary>
        /// Always store
        /// </summary>
        Always,
    }
}
