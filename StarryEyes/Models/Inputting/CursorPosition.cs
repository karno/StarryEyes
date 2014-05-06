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
                throw new ArgumentOutOfRangeException("index",
                    "Could not set index: " + index);
            }
            if (index < 0 && selectionLength != 0)
            {
                throw new ArgumentOutOfRangeException("selectionLength",
                    "Could not set SelectionLength as not zero when index set as -1.");
            }
            this._index = index;
            this._selectionLength = selectionLength;
        }

        public int Index
        {
            get { return this._index; }
        }

        public int SelectionLength
        {
            get { return this._selectionLength; }
        }

        public override int GetHashCode()
        {
            return _index.GetHashCode() ^ this._selectionLength.GetHashCode();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            var cp = obj as CursorPosition;
            return cp != null &&
                   (cp.Index == this._index && cp.SelectionLength == this.SelectionLength);
        }
    }
}
