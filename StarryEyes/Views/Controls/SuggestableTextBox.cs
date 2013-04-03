using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using StarryEyes.Albireo;
using StarryEyes.Annotations;

namespace StarryEyes.Views.Controls
{
    public sealed class SuggestableTextBox : TextBox
    {
        private SuggestItemProviderBase _provider;
        private readonly ListBox _candidateList;
        private readonly Popup _suggestListPopup;

        public DataTemplate ListItemTemplate
        {
            get { return (DataTemplate)GetValue(ListItemTemplateProperty); }
            set { SetValue(ListItemTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ListItemTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ListItemTemplateProperty =
            DependencyProperty.Register("ListItemTemplate", typeof(DataTemplate), typeof(SuggestableTextBox), new PropertyMetadata(OnTemplateChanged));

        private static void OnTemplateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var stb = (SuggestableTextBox)sender;
            stb._candidateList.ItemTemplate = (DataTemplate)e.NewValue;
        }

        public SuggestItemProviderBase SuggestItemProvider
        {
            get { return (SuggestItemProviderBase)GetValue(SuggestItemProviderProperty); }
            set { SetValue(SuggestItemProviderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SuggestItemProvider.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SuggestItemProviderProperty =
            DependencyProperty.Register("SuggestItemProvider", typeof(SuggestItemProviderBase), typeof(SuggestableTextBox), new PropertyMetadata(OnSuggestItemChanged));

        private static void OnSuggestItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var stb = (SuggestableTextBox)d;
            stb._provider = (SuggestItemProviderBase)e.NewValue;
            if (stb._provider != null)
            {
                var itemBinding = new Binding("CandidateCollection")
                {
                    Source = stb._provider,
                    Mode = BindingMode.OneWay
                };
                stb._candidateList.SetBinding(ItemsControl.ItemsSourceProperty, itemBinding);
                var idxBinding = new Binding("CandidateSelectionIndex")
                {
                    Source = stb._provider,
                    Mode = BindingMode.TwoWay
                };
                stb._candidateList.SetBinding(Selector.SelectedIndexProperty, idxBinding);
            }
        }

        public SuggestableTextBox()
        {
            _candidateList = new ListBox { Background = Brushes.White };
            _suggestListPopup = new Popup
            {
                IsOpen = false,
                Child = _candidateList,
                StaysOpen = true,
                MaxHeight = 420,
                Width = 250
            };
            _candidateList.PreviewKeyDown += (_, e) => OnPreviewKeyDown(e);
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!_suggestListPopup.IsOpen && e.Changes.Any(c => c.AddedLength > 0) &&
                this.Text.Length > this.CaretIndex - 1)
            {
                CheckTrigger(this.Text[this.CaretIndex - 1]);
            }
            if (_suggestListPopup.IsOpen)
            {
                UpdateText();
            }
            base.OnTextChanged(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!_suggestListPopup.IsOpen && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Space)
            {
                ShowCandidates();
                e.Handled = true;
            }
            if (_suggestListPopup.IsOpen && !e.Handled)
            {
                var key = e.Key;
                if (key == Key.ImeProcessed)
                {
                    key = e.ImeProcessedKey;
                }
                e.Handled = true;
                switch (key)
                {
                    case Key.Space:
                    case Key.Enter:
                        ApplySelected();
                        this.CloseCandidates();
                        if (e.Key == Key.Space)
                        {
                            this.SelectedText = " "; // insert space
                            this.SelectionStart++;
                            this.SelectionLength = 0;
                        }
                        break;
                    case Key.Escape:
                        this.CloseCandidates();
                        break;
                    case Key.PageUp:
                        if (_candidateList.SelectedIndex > 8)
                        {
                            _candidateList.SelectedIndex -= 8;
                        }
                        break;
                    case Key.Up:
                        if (_candidateList.SelectedIndex > 0)
                        {
                            _candidateList.SelectedIndex--;
                        }
                        break;
                    case Key.Down:
                        if (_candidateList.SelectedIndex < _candidateList.Items.Count - 1)
                        {
                            _candidateList.SelectedIndex++;
                        }
                        break;
                    case Key.PageDown:
                        if (_candidateList.SelectedIndex < _candidateList.Items.Count - 8)
                        {
                            _candidateList.SelectedIndex += 8;
                        }
                        break;
                    default:
                        e.Handled = false;
                        break;
                }
                if (_candidateList.SelectedItem != null)
                {
                    _candidateList.ScrollIntoView(_candidateList.SelectedItem);
                }
            }
            base.OnPreviewKeyDown(e);
        }

        public void CheckTrigger(char input)
        {
            if (_provider == null)
            {
                return;
            }
            if (_provider.CheckTriggerCharInputted(input))
            {
                ShowCandidates();
            }
        }

        public void UpdateText()
        {
            if (_provider == null)
            {
                return;
            }
            int tokenStart, __;
            var token = _provider.FindNearestToken(this.Text, this.CaretIndex, out tokenStart, out __);
            var caretIndexInToken = this.CaretIndex - tokenStart;
            if (token.IsNullOrEmpty())
            {
                CloseCandidates();
                return;
            }
            _provider.UpdateCandidateList(token, caretIndexInToken);
            if (_candidateList.SelectedItem != null)
            {
                _candidateList.ScrollIntoView(_candidateList.SelectedItem);
            }
        }

        public void ShowCandidates()
        {
            if (_provider == null)
            {
                CloseCandidates();
                return;
            }
            if (_suggestListPopup.Parent == null)
            {
                var panel = (Panel)this.Parent;
                panel.Children.Add(_suggestListPopup);
            }
            int tokenStart, _;
            var token = _provider.FindNearestToken(this.Text, this.CaretIndex, out tokenStart, out _);
            if (token == null) return;
            _suggestListPopup.PlacementTarget = this;
            _suggestListPopup.PlacementRectangle = this.GetRectFromCharacterIndex(tokenStart);
            _suggestListPopup.IsOpen = true;
            UpdateText();
            this.Focus();
        }

        public void ApplySelected()
        {
            if (_provider == null || !_suggestListPopup.IsOpen || _candidateList.SelectedItem == null)
            {
                CloseCandidates();
                return;
            }
            int tokenStart, tokenLength;
            _provider.FindNearestToken(this.Text, this.CaretIndex, out tokenStart, out tokenLength);
            this.SelectionStart = tokenStart;
            this.SelectionLength = tokenLength;
            var replace = _candidateList.SelectedItem.ToString();
            this.SelectedText = replace;
            this.SelectionStart = tokenStart + replace.Length;
            this.SelectionLength = 0;
        }

        public void CloseCandidates()
        {
            _suggestListPopup.IsOpen = false;
            this.Focus();
        }
    }

    public abstract class SuggestItemProviderBase : INotifyPropertyChanged
    {
        public abstract IList CandidateCollection { get; }

        [UsedImplicitly]
        public abstract int CandidateSelectionIndex { get; set; }

        public abstract string FindNearestToken(string text, int caretIndex, out int tokenStart, out int tokenLength);

        public abstract void UpdateCandidateList(string token, int offset);

        public abstract bool CheckTriggerCharInputted(char inputchar);

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
