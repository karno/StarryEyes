using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace StarryEyes.Views.Triggers
{
    public abstract class KeyTriggerBase : TriggerBase<UIElement>
    {
        protected override void OnAttached()
        {
            this.AssociatedObject.KeyUp += CheckFireAction;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.KeyUp -= CheckFireAction;
            base.OnDetaching();
        }

        protected abstract Key TargetKey { get; }

        void CheckFireAction(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == TargetKey)
            {
                InvokeActions(null);
            }
        }
    }
}
