using System;
using System.Collections.Generic;
using System.Linq;

namespace StarryEyes.InputFormats
{
    public sealed class FormatDescription
    {
        private readonly List<FormatElement> _elements = new List<FormatElement>();

        public List<FormatElement> Elements
        {
            get { return _elements; }
        }

        private List<int> _ids = new List<int>();
        internal void RegisterId(int inputId)
        {
            if (!_ids.Contains(inputId))
            {
                _ids.Add(inputId);
            }
        }

        public IEnumerable<int> Ids
        {
            get { return _ids.AsEnumerable(); }
        }

        internal IDictionary<int, string> Resolver
        {
            get { return _resolver; }
        }

        private IDictionary<int, string> _resolver;

        public string GenerateText(IDictionary<int, string> resolver)
        {
            this._resolver = resolver;
            return _elements.Select(e => e.GetText()).JoinString("");
        }

        public void AddElement(FormatElement element)
        {
            Elements.Add(element);
        }
    }
}
