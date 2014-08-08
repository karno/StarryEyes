using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace StarryEyes.Views.Behaviors
{
    public class KeyPreviewTrigger : TriggerBase<FrameworkElement>
    {
        public Key Key
        {
            get { return (Key)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register("Key", typeof(Key), typeof(KeyPreviewTrigger), new PropertyMetadata(Key.None));

        public ModifierKeys Modifiers
        {
            get { return (ModifierKeys)GetValue(ModifiersProperty); }
            set { SetValue(ModifiersProperty, value); }
        }

        public static readonly DependencyProperty ModifiersProperty =
            DependencyProperty.Register("Modifiers", typeof(ModifierKeys), typeof(KeyPreviewTrigger), new PropertyMetadata(ModifierKeys.None));

        public bool TrapEvent
        {
            get { return (bool)GetValue(TrapEventProperty); }
            set { SetValue(TrapEventProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrapEvent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrapEventProperty =
            DependencyProperty.Register("TrapEvent", typeof(bool), typeof(KeyPreviewTrigger), new PropertyMetadata(false));

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.PreviewKeyDown += PreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.PreviewKeyDown -= PreviewKeyDown;
            base.OnDetaching();
        }

        private void PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key && e.KeyboardDevice.Modifiers.HasFlag(Modifiers))
            {
                if (TrapEvent)
                {
                    e.Handled = true;
                }
                this.InvokeActions(null);
            }
        }
    }
}
