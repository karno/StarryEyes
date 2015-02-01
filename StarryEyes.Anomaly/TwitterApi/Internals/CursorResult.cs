using System;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.TwitterApi.Internals
{
    /// <summary>
    /// Describe cursored result of API Access.
    /// </summary>
    /// <typeparam name="T">Contains result</typeparam>
    internal class CursorResult<T> : ICursorResult<T>
    {
        private readonly T _result;
        private readonly long _previousCursor;
        private readonly long _nextCursor;

        public CursorResult(T result, string previousCursor, string nextCursor)
            : this(result, Int64.Parse(previousCursor), Int64.Parse(nextCursor))
        {
        }

        public CursorResult(T result, long previousCursor, long nextCursor)
        {
            this._result = result;
            this._previousCursor = previousCursor;
            this._nextCursor = nextCursor;
        }

        public T Result
        {
            get { return this._result; }
        }

        public long PreviousCursor
        {
            get { return this._previousCursor; }
        }

        public long NextCursor
        {
            get { return this._nextCursor; }
        }

        public bool CanReadPrevious
        {
            get { return PreviousCursor != 0; }
        }

        public bool CanReadNext
        {
            get { return NextCursor != 0; }
        }
    }
}
