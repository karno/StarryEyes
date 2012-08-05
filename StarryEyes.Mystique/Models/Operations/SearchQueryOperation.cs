using System;
using StarryEyes.SweetLady.Api.Parsing.JsonFormats;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Operations
{
    public class SearchQueryOperation : OperationBase<SavedSearchJson>
    {
        public SearchQueryOperation() { }

        public SearchQueryOperation(AuthenticateInfo info, string query)
        {
            this._authInfo = info;
            this._query = query;
            this._isAdd = true;
        }

        public SearchQueryOperation(AuthenticateInfo info, long removalId, string query)
        {
            this._authInfo = info;
            this._id = removalId;
            this._query = query;
            this._isAdd = false;
        }

        private AuthenticateInfo _authInfo;
        public AuthenticateInfo AuthInfo
        {
            get { return _authInfo; }
            set { _authInfo = value; }
        }

        private string _query;
        public string Query
        {
            get { return _query; }
            set { _query = value; }
        }

        private long _id;
        public long Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private bool _isAdd;
        public bool IsAdd
        {
            get { return _isAdd; }
            set { _isAdd = value; }
        }

        protected override IObservable<SavedSearchJson> RunCore()
        {
            return (_isAdd ? AuthInfo.CreateSavedSearch(_query) : AuthInfo.DestroySavedSearch(_id));
        }
    }
}
