using System.Windows.Input;

namespace StarryEyes.Views.Triggers
{
    public sealed class EnterKeyTrigger : KeyTriggerBase
    {
        protected override Key TargetKey
        {
            get { return Key.Enter; }
        }
    }
}
