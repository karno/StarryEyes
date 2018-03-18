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
            get => GetValue(DropDownContextMenuProperty) as ContextMenu;
            set => SetValue(DropDownContextMenuProperty, value);
        }

        protected override void OnClick()
        {
            if (DropDownContextMenu == null) return;
            DropDownContextMenu.PlacementTarget = this;
            DropDownContextMenu.Placement = PlacementMode.Bottom;
            DropDownContextMenu.IsOpen = !DropDownContextMenu.IsOpen;
            DropDownContextMenu.FlowDirection = FlowDirection.LeftToRight;
        }
    }
}