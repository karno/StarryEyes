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
            get { return (Brush)GetValue(ForegroundBrushProperty); }
            set { SetValue(ForegroundBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ForegroundBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundBrushProperty =
            DependencyProperty.Register("ForegroundBrush", typeof(Brush), typeof(TextBlockDragSelectBehavior),
                                        new PropertyMetadata(Brushes.Black));

        public Brush HighlightBrush
        {
            get { return (Brush)GetValue(HighlightBrushProperty); }
            set { SetValue(HighlightBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HighlightBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightBrushProperty =
            DependencyProperty.Register("HighlightBrush", typeof(Brush),
                                        typeof(TextBlockDragSelectBehavior),
                                        new PropertyMetadata(SystemColors.HighlightBrush));

        public Brush HighlightForegroundBrush
        {
            get { return (Brush)GetValue(HighlightForegroundBrushProperty); }
            set { SetValue(HighlightForegroundBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HighlightForegroundBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightForegroundBrushProperty =
            DependencyProperty.Register("HighlightForegroundBrush", typeof(Brush),
                                        typeof(TextBlockDragSelectBehavior),
                                        new PropertyMetadata(SystemColors.HighlightTextBrush));

        public string SelectedText
        {
            get { return (string)GetValue(SelectedTextProperty); }
            set { SetValue(SelectedTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedTextProperty =
            DependencyProperty.Register("SelectedText", typeof(string),
            typeof(TextBlockDragSelectBehavior),
                                        new PropertyMetadata(null));

        public ContextMenu SelectContextMenu
        {
            get { return (ContextMenu)GetValue(SelectContextMenuProperty); }
            set { SetValue(SelectContextMenuProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectContextMenu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectContextMenuProperty =
            DependencyProperty.Register("SelectContextMenu", typeof(ContextMenu),
                                        typeof(TextBlockDragSelectBehavior),
                                        new PropertyMetadata(null));

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            this.AssociatedObject.MouseMove += AssociatedObject_MouseMove;
            this.AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            this.AssociatedObject.MouseLeave += AssociatedObject_MouseLeave;
            this.AssociatedObject.Cursor = Cursors.IBeam;

        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            this.AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
            this.AssociatedObject.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            this.AssociatedObject.MouseLeave -= AssociatedObject_MouseLeave;
            base.OnDetaching();
        }

        void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(this.AssociatedObject);
            this.StartSelect();
            e.Handled = true;
        }

        void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelectingText) return;
            this.CommitSelect();
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            if (!_isSelectingText) return;
            this.CommitSelect();
            _isSelectingText = false;
            if (!String.IsNullOrEmpty(SelectedText))
            {
                e.Handled = true;
                this.SelectFinished();
            }
            else
            {
                this.FinalizeSelect();
            }
        }

        void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            this.FinalizeSelect();
        }

        private void StartSelect()
        {
            _isSelectingText = false;
            this.FinalizeSelect();
            this._initialized = true;
            _isSelectingText = true;
            _origInlines = this.AssociatedObject.Inlines.ToArray();
            var text = _origInlines
                .Select(i =>
                {
                    var run = i as Run;
                    if (run != null)
                    {
                        return run.Text;
                    }
                    var hyperlink = i as Hyperlink;
                    if (hyperlink != null)
                    {
                        return ((Run)hyperlink.Inlines.FirstInline).Text;
                    }
                    return null;
                })
                .Do(s => System.Diagnostics.Debug.WriteLine(s))
                .JoinString("");
            this.AssociatedObject.Inlines.Clear();
            this.AssociatedObject.Text = text;
            _selectionStart = this.AssociatedObject.GetPositionFromPoint(
                Mouse.GetPosition(this.AssociatedObject), true);
        }

        private void CommitSelect()
        {
            var point = this.AssociatedObject.GetPositionFromPoint(
                Mouse.GetPosition(this.AssociatedObject), true);
            var highlight = new TextRange(_selectionStart, point);
            highlight.ApplyPropertyValue(TextElement.BackgroundProperty, this.HighlightBrush);
            highlight.ApplyPropertyValue(TextElement.ForegroundProperty, this.HighlightForegroundBrush);
            var beforeRange = new TextRange(this.AssociatedObject.ContentStart, highlight.Start);
            beforeRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
            beforeRange.ApplyPropertyValue(TextElement.ForegroundProperty, this.ForegroundBrush);
            var afterRange = new TextRange(highlight.End, this.AssociatedObject.ContentEnd);
            afterRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
            afterRange.ApplyPropertyValue(TextElement.ForegroundProperty, this.ForegroundBrush);
            this.SelectedText = highlight.Text;
        }

        private void SelectFinished()
        {
            var point = this.AssociatedObject.GetPositionFromPoint(
                Mouse.GetPosition(this.AssociatedObject), true);
            if (point == null) return;
            var rect = point.GetCharacterRect(LogicalDirection.Forward);
            this.SelectContextMenu.PlacementTarget = this.AssociatedObject;
            this.SelectContextMenu.Placement = PlacementMode.RelativePoint;
            this.SelectContextMenu.HorizontalOffset = rect.X;
            this.SelectContextMenu.VerticalOffset = rect.Y;
            this.SelectContextMenu.IsOpen = true;
            RoutedEventHandler handler = null;
            handler = (sender, args) =>
            {
                if (handler != null)
                {
                    this.SelectContextMenu.Closed -= handler;
                }
                this.AssociatedObject_MouseLeave(null, null);
            };
            this.SelectContextMenu.Closed += handler;
        }

        private void FinalizeSelect()
        {
            // clear selection
            if (this._initialized && !_isSelectingText && !this.SelectContextMenu.IsOpen)
            {
                this._initialized = false;
                _selectionStart = null;
                var range = new TextRange(this.AssociatedObject.ContentStart, this.AssociatedObject.ContentEnd);
                range.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
                range.ApplyPropertyValue(TextElement.ForegroundProperty, this.ForegroundBrush);
                if (_origInlines != null)
                {
                    this.AssociatedObject.Inlines.Clear();
                    this.AssociatedObject.Inlines.AddRange(_origInlines);
                    _origInlines = null;
                }
            }
        }
    }
}
