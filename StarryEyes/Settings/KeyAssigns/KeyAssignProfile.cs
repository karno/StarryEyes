using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace StarryEyes.Settings.KeyAssigns
{
    public class KeyAssignProfile
    {
        public string Name { get; set; }

        public IDictionary<Key, IList<KeyAssign>> GlobalBindings { get; private set; }
        public IDictionary<Key, IList<KeyAssign>> TimelineBindings { get; private set; }
        public IDictionary<Key, IList<KeyAssign>> InputBindings { get; private set; }
        public IDictionary<Key, IList<KeyAssign>> SearchBindings { get; private set; }

        public static KeyAssignProfile FromFile(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var profile = new KeyAssignProfile(name);
            using (var stream = File.OpenRead(path))
            using (var reader = new StreamReader(stream))
            {
                profile.SetAssigns(EnumerableEx.Generate(
                    reader,
                    r => !r.EndOfStream,
                    r => r,
                    r => r.ReadLine() ?? ""));
            }
            return profile;
        }

        public void Save(string directory)
        {
            var path = Path.Combine(directory, Name + ".txt");
            using (var stream = File.OpenWrite(path))
            using (var writer = new StreamWriter(stream))
            {
                this.DumpText(writer);
            }
        }

        public string GetSourceText()
        {
            var sw = new StringWriter();
            this.DumpText(sw);
            return sw.ToString();
        }

        private void DumpText(TextWriter writer)
        {
            // write global
            writer.WriteLine("[Global]");
            GlobalBindings
                .SelectMany(s => s.Value)
                .ForEach(b => writer.WriteLine(b.ToString()));
            writer.WriteLine("[Timeline]");
            TimelineBindings
                .SelectMany(s => s.Value)
                .ForEach(b => writer.WriteLine(b.ToString()));
            writer.WriteLine("[Input]");
            InputBindings
                .SelectMany(s => s.Value)
                .ForEach(b => writer.WriteLine(b.ToString()));
            writer.WriteLine("[Search]");
            SearchBindings
                .SelectMany(s => s.Value)
                .ForEach(b => writer.WriteLine(b.ToString()));
        }

        public void SetAssigns(IEnumerable<string> lines)
        {
            var currentGroup = KeyAssignGroup.Global;
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
                    var binding = KeyAssign.FromString(line);
                    this.SetAssign(currentGroup, binding);
                }
            }
        }

        public KeyAssignProfile(string name)
        {
            Name = name;
            GlobalBindings = new Dictionary<Key, IList<KeyAssign>>();
            TimelineBindings = new Dictionary<Key, IList<KeyAssign>>();
            InputBindings = new Dictionary<Key, IList<KeyAssign>>();
            SearchBindings = new Dictionary<Key, IList<KeyAssign>>();
        }

        public void SetAssign(KeyAssignGroup group, KeyAssign assign)
        {
            IList<KeyAssign> bindings;
            var targetDic = GetDictionary(group);
            if (targetDic.TryGetValue(assign.Key, out bindings))
            {
                bindings.Add(assign);
            }
            else
            {
                targetDic.Add(assign.Key, new List<KeyAssign>(new[] { assign }));
            }
        }

        public IDictionary<Key, IList<KeyAssign>> GetDictionary(KeyAssignGroup group)
        {
            switch (group)
            {
                case KeyAssignGroup.Global:
                    return GlobalBindings;
                case KeyAssignGroup.Input:
                    return InputBindings;
                case KeyAssignGroup.Timeline:
                    return TimelineBindings;
                case KeyAssignGroup.Search:
                    return SearchBindings;
                default:
                    throw new ArgumentOutOfRangeException("group");
            }
        }

        public IEnumerable<KeyAssign> GetAssigns(Key key, KeyAssignGroup group)
        {
            var iteration = Enumerable.Empty<KeyAssign>();
            var dir = GetDictionary(group);
            IList<KeyAssign> bindings;
            if (dir.TryGetValue(key, out bindings))
            {
                iteration = iteration.Concat(bindings);
            }
            return iteration;
        }

        public IEnumerable<KeyAssign> FindAssignFromActionName(string actionName, KeyAssignGroup? searchFrom = null)
        {
            return EnumerateAssigns(searchFrom)
                .Where(k => k.Actions.Count() == 1 &&
                    k.Actions.First().ActionName == actionName);
        }

        public IEnumerable<KeyAssign> FindAssignFromActionName(IEnumerable<string> actionNames, KeyAssignGroup? searchFrom = null)
        {
            return EnumerateAssigns(searchFrom)
                .Where(k => k.Actions.Count() == 1 &&
                    k.Actions.Select(a => a.ActionName).SequenceEqual(actionNames));
        }

        private IEnumerable<KeyAssign> EnumerateAssigns(KeyAssignGroup? searchFrom = null)
        {
            if (searchFrom == null)
            {
                return EnumerableEx.Concat(
                    GlobalBindings.Values,
                    TimelineBindings.Values,
                    InputBindings.Values,
                    SearchBindings.Values)
                                   .SelectMany(items => items);
            }
            switch (searchFrom.Value)
            {
                case KeyAssignGroup.Global:
                    return GlobalBindings.Values.SelectMany(items => items);
                case KeyAssignGroup.Timeline:
                    return TimelineBindings.Values.SelectMany(items => items);
                case KeyAssignGroup.Input:
                    return InputBindings.Values.SelectMany(items => items);
                case KeyAssignGroup.Search:
                    return SearchBindings.Values.SelectMany(items => items);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
