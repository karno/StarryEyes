using System;

namespace StarryEyes.Models.Inputting
{
    public class CursorPosition
    {
        public static readonly CursorPosition Begin = new CursorPosition(0, 0);

        public static readonly CursorPosition End = new CursorPosition(-1, 0);

        private readonly int _index;
        private readonly int _selectionLength;

        public CursorPosition(int index, int selectionLength)
        {
            if (index < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    "Could not set index: " + index);
            }
            if (index < 0 && selectionLength != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(selectionLength),
                    "Could not set SelectionLength as not zero when index set as -1.");
            }
            _index = index;
            _selectionLength = selectionLength;
        }

        public int Index => _index;

        public int SelectionLength => _selectionLength;

        public override int GetHashCode()
        {
            return _index.GetHashCode() ^ _selectionLength.GetHashCode();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            var cp = obj as CursorPosition;
            return cp != null &&
                   (cp.Index == _index && cp.SelectionLength == SelectionLength);
        }
    }
}