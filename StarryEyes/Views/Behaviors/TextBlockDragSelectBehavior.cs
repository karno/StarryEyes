using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace StarryEyes.Views.Behaviors
{
    public class TextBlockDragSelectBehavior : Behavior<TextBlock>
    {
        private bool _initialized;
        private bool _isSelectingText;
        private TextPointer _selectionStart;
        private Inline[] _origInlines;

        public Brush ForegroundBrush
        {
            get => (Brush)GetValue(ForegroundBrushProperty);
            set => SetValue(ForegroundBrushProperty, value);
        }

        // Using a DependencyProperty as the backing store for ForegroundBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundBrushProperty =
            DependencyProperty.Register("ForegroundBrush", typeof(Brush), typeof(TextBlockDragSelectBehavior),
                new PropertyMetadata(Brushes.Black));

        public Brush HighlightBrush
        {
            get => (Brush)GetValue(HighlightBrushProperty);
            set => SetValue(HighlightBrushProperty, value);
        }

        // Using a DependencyProperty as the backing store for HighlightBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightBrushProperty =
            DependencyProperty.Register("HighlightBrush", typeof(Brush),
                typeof(TextBlockDragSelectBehavior),
                new PropertyMetadata(SystemColors.HighlightBrush));

        public Brush HighlightForegroundBrush
        {
            get => (Brush)GetValue(HighlightForegroundBrushProperty);
            set => SetValue(HighlightForegroundBrushProperty, value);
        }

        // Using a DependencyProperty as the backing store for HighlightForegroundBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightForegroundBrushProperty =
            DependencyProperty.Register("HighlightForegroundBrush", typeof(Brush),
                typeof(TextBlockDragSelectBehavior),
                new PropertyMetadata(SystemColors.HighlightTextBrush));

        public string SelectedText
        {
            get => (string)GetValue(SelectedTextProperty);
            set => SetValue(SelectedTextProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectedText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedTextProperty =
            DependencyProperty.Register("SelectedText", typeof(string),
                typeof(TextBlockDragSelectBehavior),
                new PropertyMetadata(null));

        public ContextMenu SelectContextMenu
        {
            get => (ContextMenu)GetValue(SelectContextMenuProperty);
            set => SetValue(SelectContextMenuProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectContextMenu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectContextMenuProperty =
            DependencyProperty.Register("SelectContextMenu", typeof(ContextMenu),
                typeof(TextBlockDragSelectBehavior),
                new PropertyMetadata(null));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseMove += AssociatedObject_MouseMove;
            AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.MouseLeave += AssociatedObject_MouseLeave;
            AssociatedObject.Cursor = Cursors.IBeam;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
            AssociatedObject.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.MouseLeave -= AssociatedObject_MouseLeave;
            base.OnDetaching();
        }

        void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(AssociatedObject);
            StartSelect();
            e.Handled = true;
        }

        void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelectingText) return;
            CommitSelect();
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            if (!_isSelectingText) return;
            CommitSelect();
            _isSelectingText = false;
            if (!String.IsNullOrEmpty(SelectedText))
            {
                e.Handled = true;
                SelectFinished();
            }
            else
            {
                FinalizeSelect();
            }
        }

        void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            FinalizeSelect();
        }

        private void StartSelect()
        {
            _isSelectingText = false;
            FinalizeSelect();
            _initialized = true;
            _isSelectingText = true;
            _origInlines = AssociatedObject.Inlines.ToArray();
            var text = _origInlines
                .Select(i =>
                {
                    var run = i as Run;
                    if (run != null)
                    {
                        return run.Text;
                    }
                    var hyperlink = i as Hyperlink;
                    return ((Run)hyperlink?.Inlines.FirstInline)?.Text;
                })
                .Do(s => System.Diagnostics.Debug.WriteLine(s))
                .JoinString("");
            AssociatedObject.Inlines.Clear();
            AssociatedObject.Text = text;
            _selectionStart = AssociatedObject.GetPositionFromPoint(
                Mouse.GetPosition(AssociatedObject), true);
        }

        private void CommitSelect()
        {
            if (_selectionStart == null) return;
            var point = AssociatedObject.GetPositionFromPoint(
                Mouse.GetPosition(AssociatedObject), true);
            var highlight = new TextRange(_selectionStart, point);
            highlight.ApplyPropertyValue(TextElement.BackgroundProperty, HighlightBrush);
            highlight.ApplyPropertyValue(TextElement.ForegroundProperty, HighlightForegroundBrush);
            var beforeRange = new TextRange(AssociatedObject.ContentStart, highlight.Start);
            beforeRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
            beforeRange.ApplyPropertyValue(TextElement.ForegroundProperty, ForegroundBrush);
            var afterRange = new TextRange(highlight.End, AssociatedObject.ContentEnd);
            afterRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
            afterRange.ApplyPropertyValue(TextElement.ForegroundProperty, ForegroundBrush);
            SelectedText = highlight.Text;
        }

        private void SelectFinished()
        {
            var point = AssociatedObject.GetPositionFromPoint(
                Mouse.GetPosition(AssociatedObject), true);
            if (point == null) return;
            var rect = point.GetCharacterRect(LogicalDirection.Forward);
            SelectContextMenu.PlacementTarget = AssociatedObject;
            SelectContextMenu.Placement = PlacementMode.RelativePoint;
            SelectContextMenu.HorizontalOffset = rect.X;
            SelectContextMenu.VerticalOffset = rect.Y;
            SelectContextMenu.IsOpen = true;

            void Handler(object sender, RoutedEventArgs args)
            {
                SelectContextMenu.Closed -= Handler;
                AssociatedObject_MouseLeave(null, null);
            }

            SelectContextMenu.Closed += Handler;
        }

        private void FinalizeSelect()
        {
            // clear selection
            if (_initialized && !_isSelectingText && !SelectContextMenu.IsOpen)
            {
                _initialized = false;
                _selectionStart = null;
                var range = new TextRange(AssociatedObject.ContentStart, AssociatedObject.ContentEnd);
                range.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, ForegroundBrush);
                if (_origInlines != null)
                {
                    AssociatedObject.Inlines.Clear();
                    AssociatedObject.Inlines.AddRange(_origInlines);
                    _origInlines = null;
                }
            }
        }
    }
}