using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cadena.Data;
using Cadena.Data.Entities;
using JetBrains.Annotations;
using StarryEyes.Albireo.Collections;
using StarryEyes.Albireo.Helpers;
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
                if (status.Recipient == null)
                {
                    throw new ArgumentException(
                        "Inconsistent status state: Recipient is not spcified in spite of status is direct message.");
                }
                return new[] { status.Recipient.Id };
            }
            return status.Entities
                         .OfType<TwitterUserMentionEntity>()
                         .Select(e => e.Id)
                         .Where(id => id != 0);
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

        private long _listId;
        private bool _isActivated;

        public ListWatcher(ListInfo info)
        {
            _info = info;
            ReceiveManager.ListMemberChanged += OnListMemberChanged;
        }

        [CanBeNull]
        public AVLTree<long> Ids { get; } = new AVLTree<long>();

        public long ListId => _listId;

        private void OnListMemberChanged(ListInfo info)
        {
            if (info.Equals(_info))
            {
                RefreshMembers();
            }
        }

        private void RefreshMembers()
        {
            Task.Run(async () =>
            {
                if (ListId == 0)
                {
                    var listDesc = await ListProxy.GetListDescription(_info).ConfigureAwait(false);
                    if (listDesc != null)
                    {
                        _listId = listDesc.Id;
                    }
                }
                // list data is not found
                if (ListId == 0) return;

                var userIds = await ListProxy.GetListMembers(ListId).ConfigureAwait(false);
                // user data is not found
                if (userIds == null) return;

                lock (Ids)
                {
                    Ids.Clear();
                    userIds.ForEach(id => Ids.Add(id));
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