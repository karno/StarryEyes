using System.Windows.Input;

namespace StarryEyes.Views.Triggers
{
    public sealed class EscapeKeyTrigger : KeyTriggerBase
    {
        protected override Key TargetKey
        {
            get { return Key.Escape; }
        }
    }
}
