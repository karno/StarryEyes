using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using JetBrains.Annotations;

namespace StarryEyes.Views.Behaviors
{
    [UsedImplicitly]
    public class DragStartBehavior : Behavior<FrameworkElement>
    {
        private Point _origin;
        private bool _isButtonDown;

        public bool IsOverrideMouseEvent
        {
            get { return (bool)GetValue(IsOverrideMouseEventProperty); }
            set { SetValue(IsOverrideMouseEventProperty, value); }
        }

        public static readonly DependencyProperty IsOverrideMouseEventProperty =
            DependencyProperty.Register("IsOverrideMouseEvent", typeof(bool), typeof(DragStartBehavior), new PropertyMetadata(false));

        public DragDropEffects AllowedEffects
        {
            get { return (DragDropEffects)GetValue(AllowedEffectsProperty); }
            set { SetValue(AllowedEffectsProperty, value); }
        }

        public static readonly DependencyProperty AllowedEffectsProperty =
            DependencyProperty.Register("AllowedEffects", typeof(DragDropEffects), typeof(DragStartBehavior), new UIPropertyMetadata(DragDropEffects.All));

        public object DragDropData
        {
            get { return GetValue(DragDropDataProperty); }
            set { SetValue(DragDropDataProperty, value); }
        }

        public static readonly DependencyProperty DragDropDataProperty =
            DependencyProperty.Register("DragDropData", typeof(object), typeof(DragStartBehavior), new PropertyMetadata(null));


        public ICommand BeforeDragDropCommand
        {
            get { return (ICommand)GetValue(BeforeDragDropCommandProperty); }
            set { SetValue(BeforeDragDropCommandProperty, value); }
        }

        public static readonly DependencyProperty BeforeDragDropCommandProperty =
            DependencyProperty.Register("BeforeDragDropCommand", typeof(ICommand), typeof(DragStartBehavior), new PropertyMetadata(null));

        public ICommand AfterDragDropCommand
        {
            get { return (ICommand)GetValue(AfterDragDropCommandProperty); }
            set { SetValue(AfterDragDropCommandProperty, value); }
        }

        public static readonly DependencyProperty AfterDragDropCommandProperty =
            DependencyProperty.Register("AfterDragDropCommand", typeof(ICommand), typeof(DragStartBehavior), new PropertyMetadata(null));

        public ICommand OnClickCommand
        {
            get { return (ICommand)GetValue(OnClickCommandProperty); }
            set { SetValue(OnClickCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OnClickCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OnClickCommandProperty =
            DependencyProperty.Register("OnClickCommand", typeof(ICommand), typeof(DragStartBehavior), new PropertyMetadata(null));

        protected override void OnAttached()
        {
            this.AssociatedObject.PreviewMouseDown += AssociatedObject_PreviewMouseDown;
            this.AssociatedObject.PreviewMouseMove += AssociatedObject_PreviewMouseMove;
            this.AssociatedObject.PreviewMouseUp += AssociatedObject_PreviewMouseUp;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.PreviewMouseDown -= AssociatedObject_PreviewMouseDown;
            this.AssociatedObject.PreviewMouseMove -= AssociatedObject_PreviewMouseMove;
            this.AssociatedObject.PreviewMouseUp -= AssociatedObject_PreviewMouseUp;
        }

        void AssociatedObject_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _origin = e.GetPosition(this.AssociatedObject);
            _isButtonDown = true;
            if (IsOverrideMouseEvent)
            {
                e.Handled = true;
            }
        }

        void AssociatedObject_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !_isButtonDown)
            {
                return;
            }
            var point = e.GetPosition(this.AssociatedObject);
            if (CheckDistance(point, _origin))
            {
                if (this.BeforeDragDropCommand != null)
                {
                    this.BeforeDragDropCommand.Execute(null);
                }
                DragDrop.DoDragDrop(this.AssociatedObject, this.DragDropData, this.AllowedEffects);
                if (this.AfterDragDropCommand != null)
                {
                    this.AfterDragDropCommand.Execute(null);
                }
                _isButtonDown = false;
                e.Handled = true;
            }
            if (IsOverrideMouseEvent)
            {
                e.Handled = true;
            }
        }

        void AssociatedObject_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isButtonDown = false;
            if (OnClickCommand != null)
            {
                var point = e.GetPosition(this.AssociatedObject);
                if (CheckDistance(point, _origin))
                {
                    OnClickCommand.Execute(null);
                }
            }
            if (IsOverrideMouseEvent)
            {
                e.Handled = true;
            }
        }

        private bool CheckDistance(Point x, Point y)
        {
            return Math.Abs(x.X - y.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                   Math.Abs(x.Y - y.Y) >= SystemParameters.MinimumVerticalDragDistance;
        }
    }
}
