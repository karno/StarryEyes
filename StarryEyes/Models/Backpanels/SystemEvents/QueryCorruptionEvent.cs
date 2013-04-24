using System;

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
            get { return "タブ " + _tabName + " のクエリが破損していたため、フィルタが初期化されました。(" + _ex.Message + ")"; }
        }
    }
}
