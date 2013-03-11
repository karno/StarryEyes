using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace StarryEyes.Settings.KeyAssigns
{
    public class KeyAssign
    {
        static readonly Regex LineRegex =
            new Regex(@"^(?<important>!)?(?:[ \t]*(?:(?<mod_ctrl>c(?:trl|ontrol)?)|(?<mod_alt>a(?:lt)?)|(?<mod_shift>s(?:hift)?))[ \t]*[\+＋]+)*(?<key>[^\:\+]+?)\:(?<action>.+?)$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        static readonly Regex ActionsParseRegex =
            new Regex(@"^(?:\s*([^(]+?\s*(?:\("".*?(?<!\\)""\))?\s*),)+$");
        static readonly Regex ActionParseRegex =
            new Regex(@"^\s*([^(]+?)\s*(?:\(""(.*?)(?<!\\)""\))?\s*$");
        static readonly Regex NumericRegex =
            new Regex("^[0-9]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static KeyAssign FromString(string line)
        {
            var result = LineRegex.Match(line.Trim());
            if (!result.Success)
            {
                throw new ArgumentException("That line could not be parsed: \"" + line + "\"");
            }
            var important = false;
            Key key;
            var modifiers = ModifierKeys.None;

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

            var actions = ActionsParseRegex.Match(result.Groups["action"].Value + ",");
            if (!actions.Success)
            {
                throw new ArgumentException("Actions are could not be parsed: " + result.Groups["action"].Value);
            }
            var descriptions = new List<KeyAssignActionDescription>();
            foreach (var capture in actions.Groups[1].Captures.OfType<Capture>().Select(s => s.Value))
            {
                var action = ActionParseRegex.Match(capture);
                if (!action.Success)
                {
                    throw new ArgumentException("Action is could not be parsed: " + capture);
                }
                descriptions.Add(new KeyAssignActionDescription
                {
                    ActionName = action.Groups[1].Value,
                    Argument = action.Groups.Count > 2 ? action.Groups[2].Value : null
                });
            }

            return new KeyAssign(key, modifiers, descriptions, important);
        }

        public KeyAssign(Key key, ModifierKeys modifiers, IEnumerable<KeyAssignActionDescription> actions, bool handlePreview = false)
        {
            Key = key;
            Modifiers = modifiers;
            HandlePreview = handlePreview;
            Actions = actions.ToArray();
        }

        public Key Key { get; set; }

        public ModifierKeys Modifiers { get; set; }

        public bool HandlePreview { get; set; }

        public IEnumerable<KeyAssignActionDescription> Actions { get; set; }

        /// <summary>
        /// Get formatted string.
        /// </summary>
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
            builder.Append(Actions.Select(a => a.ToString()).JoinString(","));
            return builder.ToString();
        }
    }

    public sealed class KeyAssignActionDescription
    {
        /// <summary>
        /// Call action name
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Action argument
        /// </summary>
        public string Argument { get; set; }

        /// <summary>
        /// Flag of has argument information
        /// </summary>
        public bool HasArgument { get { return !String.IsNullOrEmpty(Argument); } }

        public override string ToString()
        {
            return ActionName + (HasArgument ? "(\"" + Argument + "\")" : "");
        }
    }
}