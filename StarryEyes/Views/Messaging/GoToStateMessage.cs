using Livet;
using Livet.Messaging;

namespace StarryEyes.Views.Messaging
{
    public class GoToStateMessage : ResponsiveInteractionMessage<bool?>
    {
        public GoToStateMessage(string messageKey, string stateName, bool useTransitions = true)
            : base(messageKey)
        {
            DispatcherHelper.UIDispatcher.VerifyAccess();
            StateName = stateName;
            UseTransitions = useTransitions;
        }

        public GoToStateMessage(string stateName, bool useTransitions = true)
            : this(null, stateName, useTransitions)
        {
        }

        public string StateName { get; }

        public bool UseTransitions { get; }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new GoToStateMessage(MessageKey, StateName, UseTransitions);
        }
    }
}