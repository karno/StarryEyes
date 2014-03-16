using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StarryEyes.Views.Controls
{
    public sealed class HotKeyTextBox : TextBox
    {
        public static readonly DependencyProperty KeyProperty =
        DependencyProperty.Register("Key", typeof(Key), typeof(HotKeyTextBox),
        new PropertyMetadata(Key.None, (o, e) => UpdateContent((HotKeyTextBox)o)));

        public Key Key
        {
            get { return (Key)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        public static readonly DependencyProperty ModifierKeysProperty =
            DependencyProperty.Register("ModifierKeys", typeof(ModifierKeys), typeof(HotKeyTextBox),
            new PropertyMetadata(ModifierKeys.None, (o, e) => UpdateContent((HotKeyTextBox)o)));

        public ModifierKeys ModifierKeys
        {
            get { return (ModifierKeys)GetValue(ModifierKeysProperty); }
            set { SetValue(ModifierKeysProperty, value); }
        }

        public static readonly DependencyProperty SeparatorProperty =
                    DependencyProperty.Register("Separator", typeof(string), typeof(HotKeyTextBox),
                    new PropertyMetadata(" + ", (o, e) => UpdateContent((HotKeyTextBox)o)));

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Handled) return;
            e.Handled = true;
            if (e.IsRepeat) return;
            switch (e.Key)
            {
                case Key.None:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftShift:
                case Key.RightShift:
                    return;
            }
            this.Key = e.Key;
            this.ModifierKeys = Keyboard.Modifiers;
        }

        private static void UpdateContent(HotKeyTextBox textBox)
        {
            textBox.Text = StringifyShortcutKeys(textBox.Key, textBox.ModifierKeys);
        }

        public static string StringifyShortcutKeys(Key key, ModifierKeys modifier)
        {
            var content = String.Empty;
            if (modifier.HasFlag(ModifierKeys.Control))
            {
                content += "Ctrl-";
            }

            if (modifier.HasFlag(ModifierKeys.Shift))
            {
                content += "Shift-";
            }

            if (modifier.HasFlag(ModifierKeys.Alt))
            {
                content += "Alt-";
            }

            switch (key)
            {
                case Key.D0:
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                    content += key.ToString().Substring(1);
                    break;
                case Key.NumPad0:
                case Key.NumPad1:
                case Key.NumPad2:
                case Key.NumPad3:
                case Key.NumPad4:
                case Key.NumPad5:
                case Key.NumPad6:
                case Key.NumPad7:
                case Key.NumPad8:
                case Key.NumPad9:
                    content += key.ToString().Substring(6);
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftShift:
                case Key.RightShift:
                    // ignore control keys
                    break;
                default:
                    content += key.ToString();
                    break;
            }
            return content;
        }
    }
}
