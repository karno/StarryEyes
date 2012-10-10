using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;
using StarryEyes.Models.Hub;

namespace StarryEyes.Models
{
    /// <summary>
    /// ツイート入力関連のモデル
    /// </summary>
    public static class InputAreaModel
    {
        public static void NotifyChangeFocusingTab(IEnumerable<AuthenticateInfo> infos,
            IEnumerable<string> bindHashtags)
        {
        }

        public static void SetText(IEnumerable<AuthenticateInfo> infos = null, string body = null,
            CursorPosition cursor = CursorPosition.End, TwitterStatus inReplyTo = null,
            bool focusToInputArea = true)
        {
            if (focusToInputArea)
                UIHub.SetFocusTo(FocusRequest.Tweet);
        }

        public static void SetDirectMessage(IEnumerable<AuthenticateInfo> info, TwitterUser recipient,
            bool focusToInputArea = true)
        {
            if (focusToInputArea)
                UIHub.SetFocusTo(FocusRequest.Tweet);
        }
    }
    
    public enum CursorPosition
    {
        Begin,
        End,
    }
}
