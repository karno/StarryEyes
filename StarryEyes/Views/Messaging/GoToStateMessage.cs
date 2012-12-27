using Livet.Messaging;

namespace StarryEyes.Views.Messaging
{
    public class GoToStateMessage : ResponsiveInteractionMessage<bool?>
    {
        public GoToStateMessage(string messageKey, string stateName, bool useTransitions = true)
            : base(messageKey)
        {
            this.StateName = stateName;
            this.UseTransitions = useTransitions;
        }

        public GoToStateMessage(string stateName, bool useTransitions = true)
            : this(null, stateName, useTransitions)
        {
            System.Diagnostics.Debug.WriteLine("GTSM called: " + stateName);
        }

        public string StateName { get; set; }

        public bool UseTransitions { get; set; }

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new GoToStateMessage(this.MessageKey, this.StateName, this.UseTransitions);
        }
    }
}
