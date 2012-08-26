using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xaml;

namespace StarryEyes.Mystique.Models.Store
{
    public static class StoreOnMemoryObjectPersistence<T>
    {
        /// <summary>
        /// Persistent Table of Contents/Next Index of Packets enumerable.
        /// </summary>
        /// <param name="key">persistence key</param>
        /// <param name="tocniops">ToC/NIoP enumerable</param>
        public static void MakePersistent(string key, IEnumerable<Tuple<IDictionary<T, int>, IEnumerable<int>>> tocniops)
        {
            var fn = Path.Combine(App.DataStorePath, key + ".dbs"); // database structure
            var sd = tocniops.Select(s => new ToCAndNIoP<T>(s)).ToList();
            using (var pf = new FileStream(fn, FileMode.Create, FileAccess.ReadWrite))
            {
                XamlServices.Save(pf, sd);
            }
        }

        public static IEnumerable<Tuple<IDictionary<T, int>, IEnumerable<int>>> GetPersistentData(string key)
        {
            var fn = Path.Combine(App.DataStorePath, key + ".dbs"); // database structure
            using (var pf = new FileStream(fn, FileMode.Open, FileAccess.ReadWrite))
            {
                return ((XamlServices.Load(pf) as List<ToCAndNIoP<T>>) ?? new List<ToCAndNIoP<T>>())
                    .Select(t => t.GetToCNIoP());
            }
        }
    }

    public class ToCAndNIoP<T>
    {
        public ToCAndNIoP() { }

        public ToCAndNIoP(Tuple<IDictionary<T, int>, IEnumerable<int>> tocniop)
        {
            TableOfContents = new Dictionary<T, int>(tocniop.Item1);
            NextIndexOfPackets = new List<int>(tocniop.Item2);
        }

        public Dictionary<T, int> TableOfContents { get; set; }

        public List<int> NextIndexOfPackets { get; set; }

        public Tuple<IDictionary<T, int>, IEnumerable<int>> GetToCNIoP()
        {
            return new Tuple<IDictionary<T, int>, IEnumerable<int>>(TableOfContents, NextIndexOfPackets);
        }
    }
}
