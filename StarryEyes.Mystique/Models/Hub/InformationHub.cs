using System;

namespace StarryEyes.Mystique.Models.Hub
{
    /// <summary>
    /// Publish/Manage application internal information.
    /// </summary>
    public static class InformationHub
    {
        internal static event Action<Information> OnInformationPublished;

        public static void PublishInformation(Information information)
        {
            var handler = OnInformationPublished;
            if (handler != null)
                handler(information);
        }
    }

    public sealed class Information
    {
        /// <summary>
        /// Initialize information
        /// </summary>
        /// <param name="kind">InformationKind.Notify or InformationKind.Warning</param>
        /// <param name="header">one-liner description</param>
        /// <param name="detail">detail description</param>
        public Information(InformationKind kind, string header, string detail)
        {
            if (kind == InformationKind.Error)
                throw new ArgumentException("you should use another overload.");
            this.Kind = kind;
            this.Header = header;
            this.Detail = detail;
        }

        /// <summary>
        /// Initialize information
        /// </summary>
        /// <param name="kind">InformationKind.Error</param>
        /// <param name="header">one-liner description</param>
        /// <param name="detail">detail description</param>
        /// <param name="actionDesc">description of the action</param>
        /// <param name="act">fix action</param>
        public Information(InformationKind kind, string header, string detail,
            string actionDesc, Action act)
        {
            if (kind != InformationKind.Error)
                throw new ArgumentException("you should use another overload.");
            this.Kind = kind;
            this.Header = header;
            this.Detail = detail;
            this.ActionDescription = actionDesc;
            this.UserAction = act;
        }

        /// <summary>
        /// Kind of information
        /// </summary>
        public InformationKind Kind { get; set; }

        /// <summary>
        /// Header description
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Detail description
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// flag whether user action is required for remove this notify.
        /// </summary>
        public bool IsUserActionRequired
        {
            get { return Kind == InformationKind.Error; }
        }

        /// <summary>
        /// Action for fix this notification
        /// </summary>
        public Action UserAction { get; set; }

        /// <summary>
        /// Description of the action
        /// </summary>
        public string ActionDescription { get; set; }
    }

    public enum InformationKind
    {
        /// <summary>
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
