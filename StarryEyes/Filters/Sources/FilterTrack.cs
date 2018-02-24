using System;
using Cadena.Data;
using StarryEyes.Models.Receiving;

namespace StarryEyes.Filters.Sources
{
    public class FilterTrack : FilterSourceBase
    {
        private readonly string _query;

        public FilterTrack(string query)
        {
            _query = query;
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return _ => _.Text.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public override string GetSqlQuery()
        {
            return "LOWER(Text) like LOWER('%" + _query + "%')";
        }

        private bool _isActivated;

        public override void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            ReceiveManager.RegisterStreamingQuery(_query);
        }

        public override void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            ReceiveManager.UnregisterStreamingQuery(_query);
        }

        public override string FilterKey => "track";

        public override string FilterValue => _query;
    }
}