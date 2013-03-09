using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

namespace StarryEyes.Views.Triggers
{
    public sealed class KeyBindingTrigger : TriggerBase<UIElement>
    {
        protected override void OnAttached()
        {
            this.AssociatedObject.PreviewKeyDown += HandleKeyPreview;
            this.AssociatedObject.KeyUp += HandleKey;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.PreviewKeyDown -= HandleKeyPreview;
            this.AssociatedObject.KeyUp -= HandleKey;
            base.OnDetaching();
        }

        void HandleKeyPreview(object sender, System.Windows.Input.KeyEventArgs e)
        {
        }

        void HandleKey(object sender, System.Windows.Input.KeyEventArgs e)
        {
        }
    }
}
