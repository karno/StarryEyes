using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace StarryEyes.Views.Controls
{
    public class DropDownMenuButton : Button
    {
        public static readonly DependencyProperty DropDownContextMenuProperty =
            DependencyProperty.Register("DropDownContextMenu", typeof(ContextMenu),
            typeof(DropDownMenuButton), new UIPropertyMetadata(null));

        public ContextMenu DropDownContextMenu
        {
            get { return this.GetValue(DropDownContextMenuProperty) as ContextMenu; }
            set { this.SetValue(DropDownContextMenuProperty, value); }
        }

        protected override void OnClick()
        {
            if (this.DropDownContextMenu == null) return;
            this.DropDownContextMenu.PlacementTarget = this;
            this.DropDownContextMenu.Placement = PlacementMode.Bottom;
            this.DropDownContextMenu.IsOpen = !this.DropDownContextMenu.IsOpen;
            this.DropDownContextMenu.FlowDirection = FlowDirection.LeftToRight;
        }
    }
}
