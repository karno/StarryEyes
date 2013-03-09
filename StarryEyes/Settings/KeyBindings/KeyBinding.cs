using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace StarryEyes.Settings.KeyBindings
{
    public class KeyBinding
    {
        static readonly Regex LineRegex =
            new Regex(@"^(?<important>!)?(?:[ \t]*(?:(?<mod_ctrl>c(?:trl|ontrol)?)|(?<mod_alt>a(?:lt)?)|(?<mod_shift>s(?:hift)?))[ \t]*[\+＋]+)*(?<key>[^\:\+]+?)\:(?<action>.+?)$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        static readonly Regex NumericRegex =
            new Regex("^[0-9]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static KeyBinding FromString(string line)
        {
            var result = LineRegex.Match(line.Trim());
            if (!result.Success)
            {
                throw new ArgumentException("That line could not be parsed: \"" + line + "\"");
            }
            var important = false;
            var key = Key.None;
            var modifiers = ModifierKeys.None;
            string action = result.Groups["action"].Value;

            if (result.Groups["important"].Success)
            {
                important = true;
            }

            // pre-convert numeric keys
            var keystr = result.Groups["key"].Value;
            if (NumericRegex.IsMatch(keystr))
                keystr = "D" + keystr; // 0 -> D0, 1 -> D1, ...

            if (!Enum.TryParse(keystr, true, out key))
            {
                throw new ArgumentException("That key is not defined: " + keystr);
            }

            if (result.Groups["mod_ctrl"].Success)
            {
                modifiers |= ModifierKeys.Control;
            }

            if (result.Groups["mod_alt"].Success)
            {
                modifiers |= ModifierKeys.Alt;
            }

            if (result.Groups["mod_shift"].Success)
            {
                modifiers |= ModifierKeys.Shift;
            }

            return new KeyBinding(key, modifiers, action, important);
        }

        public KeyBinding(Key key, ModifierKeys modifiers, string callAction, bool handlePreview = false)
        {
            Key = key;
            Modifiers = modifiers;
            CallAction = callAction;
            HandlePreview = handlePreview;
        }

        public Key Key { get; set; }

        public ModifierKeys Modifiers { get; set; }

        public bool HandlePreview { get; set; }

        public string CallAction { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (HandlePreview)
                builder.Append("!");
            if (Modifiers.HasFlag(ModifierKeys.Control))
                builder.Append("Ctrl+");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                builder.Append("Alt+");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                builder.Append("Shift+");
            builder.Append(Key.ToString());
            builder.Append(": ");
            builder.Append(CallAction);
            return builder.ToString();
        }
    }
}