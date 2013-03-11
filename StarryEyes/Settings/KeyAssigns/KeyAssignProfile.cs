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
                // write global
                // ReSharper disable AccessToDisposedClosure
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
                // ReSharper restore AccessToDisposedClosure
            }
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
            if (group != KeyAssignGroup.Global)
            {
                IList<KeyAssign> globalBindings;
                if (GlobalBindings.TryGetValue(key, out globalBindings))
                {
                    iteration = iteration.Concat(globalBindings);
                }
            }
            return iteration;
        }
    }
}
