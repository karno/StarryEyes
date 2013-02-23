using System;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Connections.UserDependencies;

namespace StarryEyes.Filters.Sources
{
    public class FilterTrack : FilterSourceBase
    {
        private readonly string _query;
        public FilterTrack(string query)
        {
            this._query = query;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return _ => _.Text.IndexOf(_query, StringComparison.Ordinal) >= 0;
        }

        private bool _isActivated;
        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            UserBaseConnectionsManager.AddTrackKeyword(_query);
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            UserBaseConnectionsManager.RemoveTrackKeyword(_query);
        }

        public override string FilterKey
        {
            get { return "track"; }
        }

        public override string FilterValue
        {
            get { return _query; }
        }
    }
}
