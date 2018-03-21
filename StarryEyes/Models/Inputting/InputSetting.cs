using System;
using System.Collections.Generic;
using System.Linq;
using Cadena.Data;
using Cadena.Data.Entities;
using JetBrains.Annotations;
using StarryEyes.Models.Accounting;
using StarryEyes.Settings;

namespace StarryEyes.Models.Inputting
{
    /// <summary>
    /// Describe input box setup state
    /// </summary>
    public class InputSetting
    {
        public static InputSetting Create(string body)
        {
            return new InputSetting
            {
                Body = body
            };
        }

        public static InputSetting Create(IEnumerable<TwitterAccount> accounts, string body)
        {
            return new InputSetting
            {
                Accounts = accounts,
                Body = body
            };
        }

        public static InputSetting CreateReply([CanBeNull] TwitterStatus inReplyTo, string body = null,
            bool addMentions = true)
        {
            if (inReplyTo == null) throw new ArgumentNullException(nameof(inReplyTo));
            var reply = GetSuitableReplyAccount(inReplyTo);
            var except = reply == null ? new[] { inReplyTo.User.Id } : new[] { reply.Id, inReplyTo.User.Id };
            var mention = String.Empty;
            if (addMentions)
            {
                mention = "@" + inReplyTo.User.ScreenName + " " +
                          inReplyTo.Entities
                                   .Guard()
                                   .OfType<TwitterUserMentionEntity>()
                                   .Where(e => !except.Contains(e.Id))
                                   .Select(e => e.DisplayText)
                                   .Distinct()
                                   .JoinString(" ");
            }
            return new InputSetting
            {
                Accounts = reply == null ? null : new[] { reply },
                Body = mention + body,
                InReplyTo = inReplyTo,
                CursorPosition = new CursorPosition(inReplyTo.User.ScreenName.Length + 2, (mention + body).Length)
            };
        }

        public static InputSetting CreateDirectMessage([CanBeNull] TwitterUser recipient, string body = null)
        {
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            return new InputSetting
            {
                Recipient = recipient,
                Body = body
            };
        }

        public static InputSetting CreateDirectMessage(
            IEnumerable<TwitterAccount> accounts, [CanBeNull] TwitterUser recipient, string body = null)
        {
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            return new InputSetting
            {
                Accounts = accounts,
                Recipient = recipient,
                Body = body
            };
        }

        [CanBeNull]
        private static TwitterAccount GetSuitableReplyAccount(TwitterStatus status)
        {
            if (status.StatusType == StatusType.DirectMessage && status.Recipient != null)
            {
                return Setting.Accounts.Get(status.Recipient.Id);
            }
            var replyTargets = status.Entities
                                     .Guard()
                                     .OfType<TwitterUserMentionEntity>()
                                     .Select(e => e.Id)
                                     .ToArray();
            var account = Setting.Accounts.Collection
                                 .FirstOrDefault(a => replyTargets.Contains(a.Id));
            return account != null ? BacktrackFallback(account) : null;
        }

        [CanBeNull]
        private static TwitterAccount BacktrackFallback([CanBeNull] TwitterAccount account)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            if (!Setting.IsBacktrackFallback.Value)
            {
                return account;
            }
            var cinfo = account;
            while (true)
            {
                var backtrack = Setting.Accounts.Collection.FirstOrDefault(a => a.FallbackAccountId == cinfo.Id);
                if (backtrack == null)
                {
                    return cinfo;
                }
                if (backtrack.Id == account.Id)
                {
                    return account;
                }
                cinfo = backtrack;
            }
        }

        private IEnumerable<TwitterAccount> _accounts;
        private TwitterUser _recipient;
        private TwitterStatus _inReplyTo;

        [CanBeNull]
        public IEnumerable<TwitterAccount> Accounts
        {
            get => _accounts;
            set => _accounts = value?.ToArray();
        }

        [CanBeNull]
        public string Body { get; set; }

        [CanBeNull]
        public TwitterUser Recipient
        {
            get => _recipient;
            set
            {
                if (value != null && _inReplyTo != null)
                {
                    throw new InvalidOperationException("Can not set Recipient and InReplyTo simultaneously.");
                }
                _recipient = value;
            }
        }

        [CanBeNull]
        public TwitterStatus InReplyTo
        {
            get => _inReplyTo;
            set
            {
                if (value != null && _recipient != null)
                {
                    throw new InvalidOperationException("Can not set Recipient and InReplyTo simultaneously.");
                }
                _inReplyTo = value;
            }
        }

        [CanBeNull]
        public CursorPosition CursorPosition { get; set; }

        public bool SetFocusToInputArea { get; }

        private InputSetting()
        {
            SetFocusToInputArea = true;
            CursorPosition = CursorPosition.End;
        }
    }
}