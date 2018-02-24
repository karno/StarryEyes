using System;
using System.Collections.Generic;
using System.Linq;
using Cadena.Data;
using StarryEyes.Albireo.Collections;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models.Receiving;

namespace StarryEyes.Filters.Expressions.Values.Lists
{
    public class ListMembers : ValueBase
    {
        private readonly ListInfo _listInfo;

        private readonly ListWatcher _watcher;

        private bool _isAlive;

        public ListMembers(string userScreenName, string listSlug)
        {
            _listInfo = new ListInfo(userScreenName, listSlug);
            _watcher = new ListWatcher(_listInfo);
            _watcher.OnListMemberUpdated += RaiseInvalidateFilter;
        }

        public override string ToQuery()
        {
            return "list." + _listInfo.OwnerScreenName + "." +
                   (_listInfo.Slug.EscapeForQuery().Quote());
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Set; }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var tempList = new AVLTree<long>();
            lock (_watcher.Ids)
            {
                _watcher.Ids.ForEach(tempList.Add);
            }
            return _ => tempList;
        }

        public override string GetSetSqlQuery()
        {
            return "(select UserId from ListUser where ListId = " + _watcher.ListId + ")";
        }

        public override void BeginLifecycle()
        {
            if (_isAlive) return;
            _isAlive = true;
            _watcher.Activate();
            ReceiveManager.RegisterListMember(_listInfo);
        }

        public override void EndLifecycle()
        {
            if (!_isAlive) return;
            _isAlive = false;
            _watcher.Deactivate();
            ReceiveManager.UnregisterListMember(_listInfo);
        }
    }
}