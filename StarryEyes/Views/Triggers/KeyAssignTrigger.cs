using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using StarryEyes.Settings;
using StarryEyes.Settings.KeyAssigns;

namespace StarryEyes.Views.Triggers
{
    public sealed class KeyAssignTrigger : TriggerBase<UIElement>
    {
        public KeyAssignGroup Group
        {
            get { return (KeyAssignGroup)GetValue(GroupProperty); }
            set { SetValue(GroupProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Group.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GroupProperty =
            DependencyProperty.Register("Group", typeof(KeyAssignGroup), typeof(KeyAssignTrigger), new PropertyMetadata(KeyAssignGroup.Global));

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

        void HandleKeyPreview(object sender, KeyEventArgs e)
        {
            e.Handled = CheckActionKey(e.Key, true);
        }

        void HandleKey(object sender, KeyEventArgs e)
        {
            e.Handled = CheckActionKey(e.Key, false);
        }

        private bool CheckActionKey(Key key, bool isPreview)
        {
            return KeyAssignManager.CurrentProfile.GetAssigns(key, Group)
                             .Where(b => (b.HandlePreview && isPreview) || !isPreview)
                             .Where(b => b.Modifiers == Keyboard.Modifiers)
                             .SelectMany(b => b.Actions)
                             .Select(KeyAssignManager.InvokeAction)
                             .ToArray()
                             .Any(r => r);
        }
    }
}
