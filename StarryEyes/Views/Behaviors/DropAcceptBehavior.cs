using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using JetBrains.Annotations;
using StarryEyes.ViewModels;
using StarryEyes.Views.Adorners;

namespace StarryEyes.Views.Behaviors
{
    [UsedImplicitly]
    public sealed class DropAcceptBehavior : Behavior<FrameworkElement>
    {
        public DropAcceptDescription Description
        {
            get => (DropAcceptDescription)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        // Using a DependencyProperty as the backing store for Description.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(DropAcceptDescription), typeof(DropAcceptBehavior),
                new PropertyMetadata(null));

        protected override void OnAttached()
        {
            AssociatedObject.PreviewDragOver += AssociatedObject_DragOver;
            AssociatedObject.PreviewDrop += AssociatedObject_Drop;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewDragOver -= AssociatedObject_DragOver;
            AssociatedObject.PreviewDrop -= AssociatedObject_Drop;
            base.OnDetaching();
        }

        void AssociatedObject_DragOver(object sender, DragEventArgs e)
        {
            var desc = Description;
            if (desc == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            desc.OnDragOver(e);
            e.Handled = true;
        }

        void AssociatedObject_Drop(object sender, DragEventArgs e)
        {
            var desc = Description;
            if (desc == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            desc.OnDrop(e);
            e.Handled = true;
        }
    }

    public sealed class AdornedDropAcceptBehavior : Behavior<ItemsControl>
    {
        public DropAcceptDescription Description
        {
            get => (DropAcceptDescription)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        // Using a DependencyProperty as the backing store for Description.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(DropAcceptDescription),
                typeof(AdornedDropAcceptBehavior), new PropertyMetadata(null));

        protected override void OnAttached()
        {
            AssociatedObject.PreviewDragOver += AssociatedObject_DragOver;
            AssociatedObject.PreviewDrop += AssociatedObject_Drop;
            AssociatedObject.PreviewDragEnter += AssociatedObject_PreviewDragEnter;
            AssociatedObject.PreviewDragLeave += AssociatedObject_PreviewDragLeave;
            AssociatedObject.PreviewMouseUp += AssociatedObject_PreviewMouseUp;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewDragOver -= AssociatedObject_DragOver;
            AssociatedObject.PreviewDrop -= AssociatedObject_Drop;
            AssociatedObject.PreviewDragEnter -= AssociatedObject_PreviewDragEnter;
            AssociatedObject.PreviewDragLeave -= AssociatedObject_PreviewDragLeave;
            AssociatedObject.PreviewMouseUp -= AssociatedObject_PreviewMouseUp;
            base.OnDetaching();
        }

        void AssociatedObject_DragOver(object sender, DragEventArgs e)
        {
            var desc = Description;
            if (desc == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            desc.OnDragOver(e);
            e.Handled = true;
        }

        void AssociatedObject_Drop(object sender, DragEventArgs e)
        {
            var desc = Description;
            if (desc == null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            desc.OnDrop(e);
            DestroyInsertionAdorner();
            e.Handled = true;
        }

        private InsertionAdorner _insertionAdorner;

        void AssociatedObject_PreviewDragEnter(object sender, DragEventArgs e)
        {
            CreateInsertionAdorner(sender as ItemsControl, e.OriginalSource as DependencyObject);
        }

        void AssociatedObject_PreviewDragLeave(object sender, DragEventArgs e)
        {
            DestroyInsertionAdorner();
        }

        void AssociatedObject_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DestroyInsertionAdorner();
        }

        private void CreateInsertionAdorner(ItemsControl itemsControl, DependencyObject dependencyObject)
        {
            DestroyInsertionAdorner();
            if (itemsControl == null || dependencyObject == null)
            {
                return;
            }
            var container = itemsControl.ContainerFromElement(dependencyObject) as FrameworkElement;
            if (container != null)
            {
                _insertionAdorner = new InsertionAdorner(container);
                return;
            }
            var lastcontainer = itemsControl.ItemContainerGenerator
                                            .ContainerFromIndex(itemsControl.Items.Count - 1) as FrameworkElement;
            if (lastcontainer != null)
            {
                _insertionAdorner = new InsertionAdorner(lastcontainer, true);
            }
        }

        private void DestroyInsertionAdorner()
        {
            if (_insertionAdorner != null)
            {
                _insertionAdorner.Dispose();
                _insertionAdorner = null;
            }
        }
    }
}