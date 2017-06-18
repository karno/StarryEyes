using JetBrains.Annotations;

namespace Sophia.Messaging.UI
{
    public class GoToStateMessage : ResponsiveMessageBase<bool>
    {
        public GoToStateMessage([CanBeNull] string messageKey, string stateName, bool useTransitions = true)
            : base(messageKey)
        {
            StateName = stateName;
            UseTransitions = useTransitions;
        }

        public GoToStateMessage(string stateName, bool useTransitions = true)
            : this(null, stateName, useTransitions)
        {
        }

        public string StateName { get; }

        public bool UseTransitions { get; }
    }
}