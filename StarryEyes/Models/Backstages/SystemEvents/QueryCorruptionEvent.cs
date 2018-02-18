using System;
using StarryEyes.Globalization;

namespace StarryEyes.Models.Backstages.SystemEvents
{
    public sealed class QueryCorruptionEvent : SystemEventBase
    {
        private readonly string _tabName;
        private readonly Exception _ex;

        public QueryCorruptionEvent(string tabName, Exception ex)
        {
            _tabName = tabName;
            _ex = ex;
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Error; }
        }

        public override string Detail
        {
            get { return BackstageResources.QueryCorruptedFormat.SafeFormat(_tabName, _ex.Message); }
        }
    }
}
