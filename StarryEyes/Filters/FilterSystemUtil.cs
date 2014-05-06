using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Albireo;
using StarryEyes.Albireo.Collections;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters.Expressions.Operators;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving;

namespace StarryEyes.Filters
{
    public static class FilterSystemUtil
    {
        public static IEnumerable<long> InReplyToUsers(TwitterStatus status)
        {
            if (status.StatusType == StatusType.DirectMessage)
            {
                return new[] { status.Recipient.Id };
            }
            if (status.Entities == null)
            {
                return Enumerable.Empty<long>();
            }
            return status.Entities
                         .Where(e => e.EntityType == EntityType.UserMentions)
                         .Select(e => e.UserId ?? 0)
                         .Where(id => id != 0);
        }

        public static TwitterStatus GetOriginal(this TwitterStatus status)
        {
            return status.RetweetedOriginal ?? status;
        }

        public static FilterOperatorBase And(this FilterOperatorBase left, FilterOperatorBase right)
        {
            if (left is FilterOperatorOr)
            {
                left = new FilterBracket { Value = left };
            }
            if (right is FilterOperatorOr)
            {
                right = new FilterBracket { Value = right };
            }
            return new FilterOperatorAnd
            {
                LeftValue = left,
                RightValue = right
            };
        }

        public static FilterOperatorBase Or(this FilterOperatorBase left, FilterOperatorBase right)
        {
            return new FilterOperatorOr
            {
                LeftValue = left,
                RightValue = right
            };
        }
    }

    public class ListWatcher
    {
        private readonly ListInfo _info;
        public event Action OnListMemberUpdated;

        private readonly AVLTree<long> _ids = new AVLTree<long>();
        private long _listId;
        private bool _isActivated;

        public ListWatcher(ListInfo info)
        {
            this._info = info;
            ReceiveManager.ListMemberChanged += OnListMemberChanged;
        }

        [NotNull]
        public AVLTree<long> Ids
        {
            get { return this._ids; }
        }

        public long ListId
        {
            get { return this._listId; }
        }

        private void OnListMemberChanged(ListInfo info)
        {
            if (info.Equals(this._info))
            {
                RefreshMembers();
            }
        }

        private void RefreshMembers()
        {
            Task.Run(async () =>
            {
                if (this.ListId == 0)
                {
                    var listDesc = await ListProxy.GetListDescription(_info);
                    if (listDesc != null)
                    {
                        this._listId = listDesc.Id;
                    }
                }
                // list data is not found
                if (this.ListId == 0) return;

                var userIds = await ListProxy.GetListMembers(this.ListId);
                // user data is not found
                if (userIds == null) return;

                lock (this.Ids)
                {
                    this.Ids.Clear();
                    userIds.ForEach(id => this.Ids.Add(id));
                }
                OnListMemberUpdated.SafeInvoke();
            });
        }

        public void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            ReceiveManager.ListMemberChanged += OnListMemberChanged;
            RefreshMembers();
        }

        public void Deactivate()
        {
            if (!_isActivated) return;
            _isActivated = false;
            // remove list subscription
            ReceiveManager.ListMemberChanged -= OnListMemberChanged;
        }
    }
}
