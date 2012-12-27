using System;

namespace StarryEyes.Vanille.DataStore
{
    public class FindRange<T> where T : IComparable<T>
    {
        /// <summary>
        /// Initialize range with from mode.
        /// </summary>
        /// <param name="value">from value</param>
        /// <returns>range instance</returns>
        public static FindRange<T> From(T value)
        {
            return new FindRange<T>(value, default(T), RangeMode.From);
        }

        /// <summary>
        /// Initialize range with by mode.
        /// </summary>
        /// <param name="value">by value</param>
        /// <returns>range instance</returns>
        public static FindRange<T> By(T value)
        {
            return new FindRange<T>(default(T), value, RangeMode.By);
        }

        /// <summary>
        /// Initialize range with between mode.
        /// </summary>
        /// <param name="from">from value</param>
        /// <param name="by">by value</param>
        /// <returns>range instance</returns>
        public static FindRange<T> Between(T from, T by)
        {
            return new FindRange<T>(from, by, RangeMode.Between);
        }

        private readonly T _begin;
        private readonly T _end;
        private readonly RangeMode _mode;

        private FindRange(T begin, T end, RangeMode mode)
        {
            this._begin = begin;
            this._end = end;
            this._mode = mode;
        }

        /// <summary>
        /// Get from value
        /// </summary>
        public T Begin
        {
            get
            {
                if (_mode == RangeMode.By)
                    throw new Exception("cannot read begin field when in by range mode.");
                return _begin;
            }
        }

        /// <summary>
        /// Get to value
        /// </summary>
        public T End
        {
            get
            {
                if (_mode == RangeMode.From)
                    throw new Exception("cannot read end field when in from range mode.");
                return _end;
            }
        }

        /// <summary>
        /// Get range mode
        /// </summary>
        public RangeMode Mode
        {
            get { return _mode; }
        }

        /// <summary>
        /// Check is in
        /// </summary>
        public bool IsIn(T key)
        {
            switch (this.Mode)
            {
                case RangeMode.From:
                    return key.CompareTo(this.Begin) >= 0;
                case RangeMode.By:
                    return key.CompareTo(this.End) <= 0;
                case RangeMode.Between:
                    return key.CompareTo(this.Begin) >= 0 && key.CompareTo(this.End) <= 0;
                default:
                    return false;
            }
        }

    }

    public enum RangeMode
    {
        /// <summary>
        /// set from
        /// </summary>
        From,
        /// <summary>
        /// set by
        /// </summary>
        By,
        /// <summary>
        /// between
        /// </summary>
        Between,
    }
}
