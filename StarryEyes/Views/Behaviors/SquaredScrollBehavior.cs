using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using StarryEyes.Nightmare.Windows;

namespace StarryEyes.Views.Behaviors
{
    public sealed class SquaredScrollBehavior : Behavior<ScrollViewer>
    {
        protected override void OnAttached()
        {
            this.AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
        }

        private int _integrate;
        private void AssociatedObject_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;
            // delta ^ 2
            // integrate mouse whell delta
            _integrate += e.Delta;
            int dp = _integrate / SystemInformation.MouseWheelScrollDelta;
            _integrate %= SystemInformation.MouseWheelScrollDelta;
            if (dp != 0)
            {
                this.AssociatedObject.ScrollToVerticalOffset(
                    this.AssociatedObject.VerticalOffset -
                    (dp * Math.Abs(dp)) * ScrollUnit);
            }
        }

        private int ScrollUnit
        {
            get { return SystemParameters.WheelScrollLines * SystemInformation.MouseWheelScrollDelta / 6; }
        }
    }
}
