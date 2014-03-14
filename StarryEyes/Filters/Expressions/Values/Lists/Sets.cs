using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Albireo.Collections;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving;

namespace StarryEyes.Filters.Expressions.Values.Lists
{
    public class ListMembers : ValueBase
    {
        private readonly ListInfo _listInfo;

        private long _listId;

        public ListMembers(string userScreenName, string listSlug)
        {
            _listInfo = new ListInfo(userScreenName, listSlug);
        }

        public override string ToQuery()
        {
            return "list." + _listInfo.OwnerScreenName +
                   (_listInfo.Slug.EscapeForQuery().Quote());
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Set;
            }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var tempList = new AVLTree<long>();
            lock (_ids)
            {
                _ids.ForEach(tempList.Add);
            }
            return _ => tempList;
        }

        public override string GetSetSqlQuery()
        {
            return "(select UserId from ListUser where ListId = " + _listId + ")";
        }

        public override void BeginLifecycle()
        {
            ReceiveManager.ListMemberChanged += this.OnListMemberChanged;
            ReceiveManager.RegisterListMember(_listInfo);
            RefreshMembers();
        }

        public override void EndLifecycle()
        {
            ReceiveManager.ListMemberChanged -= this.OnListMemberChanged;
            ReceiveManager.UnregisterListMember(_listInfo);
        }

        void OnListMemberChanged(ListInfo info)
        {
            if (info.Equals(_listInfo))
            {
                RefreshMembers();
            }
        }

        #region Manage members

        private readonly AVLTree<long> _ids = new AVLTree<long>();

        private void RefreshMembers()
        {
            Task.Run(async () =>
            {
                var listDesc = await ListProxy.GetListDescription(_listInfo);
                if (listDesc != null)
                {
                    _listId = listDesc.Id;
                }
                var userIds = await ListProxy.GetListMembers(_listInfo);
                if (userIds == null) return;
                lock (_ids)
                {
                    _ids.Clear();
                    userIds.ForEach(id => _ids.Add(id));
                }
                RaiseReapplyFilter();
            });
        }

        #endregion
    }
}
