using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace StarryEyes.Settings.KeyBindings
{
    public class KeyBindingProfile
    {
        public string Name { get; set; }

        public IDictionary<Key, KeyBinding> GlobalBindings { get; private set; }
        public IDictionary<Key, KeyBinding> TimelineBindings { get; private set; }
        public IDictionary<Key, KeyBinding> InputBindings { get; private set; }
        public IDictionary<Key, KeyBinding> SearchBindings { get; private set; }

        public static KeyBindingProfile FromFile(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var profile = new KeyBindingProfile(name);
            using (var stream = File.OpenRead(path))
            using (var reader = new StreamReader(stream))
            {
                profile.SetBindings(EnumerableEx.Generate(
                    reader,
                    r => !r.EndOfStream,
                    r => r,
                    r => r.ReadLine() ?? ""));
            }
            return profile;
        }

        public void SetBindings(IEnumerable<string> lines)
        {
            var currentGroup = BindingGroup.Global;
            foreach (var line in lines)
            {
                if (String.IsNullOrWhiteSpace(line) ||
                    line.StartsWith("#"))
                {
                    // empty line or comment
                    continue;
                }
                if (line.StartsWith("["))
                {
                    // grouping tag
                    if (!Enum.TryParse(line.Substring(1, line.Length - 2), true, out currentGroup))
                    {
                        throw new ArgumentException("This group tag is not valid. :" + line);
                    }
                }
                else
                {
                    var binding = KeyBinding.FromString(line);
                    this.SetBinding(currentGroup, binding);
                }
            }
        }

        public KeyBindingProfile(string name)
        {
            Name = name;
            GlobalBindings = new SortedDictionary<Key, KeyBinding>();
            TimelineBindings = new SortedDictionary<Key, KeyBinding>();
            InputBindings = new SortedDictionary<Key, KeyBinding>();
            SearchBindings = new SortedDictionary<Key, KeyBinding>();
        }

        public void SetBinding(BindingGroup group, KeyBinding binding)
        {
            GetDictionary(group).Add(binding.Key, binding);
        }

        public IDictionary<Key, KeyBinding> GetDictionary(BindingGroup group)
        {
            switch (group)
            {
                case BindingGroup.Global:
                    return GlobalBindings;
                case BindingGroup.Input:
                    return InputBindings;
                case BindingGroup.Timeline:
                    return TimelineBindings;
                case BindingGroup.Search:
                    return SearchBindings;
                default:
                    throw new ArgumentOutOfRangeException("group");
            }
        }
    }
}
