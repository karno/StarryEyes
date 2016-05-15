using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using StarryEyes.Filters.Parsing;
using StarryEyes.Globalization.Filters;
using StarryEyes.Views.Controls.QueryEditorResources;

namespace StarryEyes.Views.Controls
{
    public class QueryEditor : TextEditor
    {
        public string QueryText
        {
            get { return (string)GetValue(QueryTextProperty); }
            set { SetValue(QueryTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for QueryText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty QueryTextProperty =
            DependencyProperty.Register("QueryText", typeof(string), typeof(QueryEditor), new PropertyMetadata(null, QueryTextChangedStatic));

        private static void QueryTextChangedStatic(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var qe = obj as QueryEditor;
            if (qe == null || qe.Text == (string)e.NewValue) return;
            qe.Text = (string)e.NewValue;
        }

        public bool IsSourceFilterEditable
        {
            get { return (bool)GetValue(IsSourceFilterEditableProperty); }
            set { SetValue(IsSourceFilterEditableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSourceFilterEditable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSourceFilterEditableProperty =
            DependencyProperty.Register("IsSourceFilterEditable", typeof(bool), typeof(QueryEditor), new PropertyMetadata(true));

        public QueryEditor()
        {
            this.ShowLineNumbers = true;
            this.LoadXshd();
            this.TextArea.TextEntering += TextArea_TextEntering;
            this.TextArea.TextEntered += TextArea_TextEntered;
            this.ContextMenu = this.BuildContextMenu();
        }


        private ContextMenu BuildContextMenu()
        {
            var cm = new ContextMenu();
            cm.Items.Add(new MenuItem { Command = ApplicationCommands.Undo });
            cm.Items.Add(new MenuItem { Command = ApplicationCommands.Redo });
            cm.Items.Add(new Separator());
            cm.Items.Add(new MenuItem { Command = ApplicationCommands.Cut });
            cm.Items.Add(new MenuItem { Command = ApplicationCommands.Copy });
            cm.Items.Add(new MenuItem { Command = ApplicationCommands.Paste });
            cm.Items.Add(new MenuItem { Command = ApplicationCommands.Delete });
            cm.Items.Add(new Separator());
            cm.Items.Add(new MenuItem { Command = ApplicationCommands.SelectAll });
            return cm;
        }

        private const string ResourceName = "StarryEyes.Views.Controls.QueryEditorResources.KrileQuery.xshd";

        private void LoadXshd()
        {
            var asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream(ResourceName) ?? Stream.Null)
            using (var reader = XmlReader.Create(stream))
            {
                this.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            QueryText = this.Text;
            base.OnTextChanged(e);
        }

        #region code completion

        private CompletionWindow _completionWindow;

        private void OpenCompletionWindow(IEnumerable<CompletionData> completions)
        {
            if (this._completionWindow != null) return;
            var wnd = new CompletionWindow(this.TextArea);
            wnd.Closed += (o, e) =>
            {
                if (this._completionWindow.Equals(wnd))
                {
                    this._completionWindow = null;
                }
            };
            completions.ForEach(wnd.CompletionList.CompletionData.Add);
            // insert elements
            this._completionWindow = wnd;
            wnd.Show();
        }

        void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            var cidx = CaretOffset;
            if (cidx < 0 || this._completionWindow != null) return;
            var cd = this.QueryCompletionData(this.Text.Substring(0, cidx), e.Text);
            if (cd != null)
            {
                this.OpenCompletionWindow(cd);
            }
        }

        void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && this._completionWindow != null &&
                !char.IsLetterOrDigit(e.Text[0]))
            {
                this._completionWindow.CompletionList.RequestInsertion(e);
            }
        }

        private IEnumerable<CompletionData> QueryCompletionData(string beforeCursor, string inputted)
        {
            var tokens = Tokenizer.Tokenize(beforeCursor, true).ToArray();
            if (!IsSourceFilterEditable)
            {
                return this.QueryOnRight(tokens, inputted);
            }
            var wheres = tokens.SkipWhile(t => !t.IsMatchTokenLiteral("where"))
                               .ToArray();
            if (wheres.Length > 0)
            {
                return this.QueryOnRight(wheres.Skip(1), inputted);
            }
            var froms = tokens.SkipWhile(t => !t.IsMatchTokenLiteral("from"))
                              .ToArray();
            if (froms.Length > 0)
            {
                return this.QueryOnLeft(froms.Skip(1), inputted);
            }
            switch (inputted.ToLower())
            {
                case "f":
                    return new[] { new CompletionData("from", "rom", QueryCompletionResources.KeywordFrom) };
                case "w":
                    return new[] { new CompletionData("where", "here", QueryCompletionResources.KeywordWhere) };
            }
            return null;
        }

        private IEnumerable<CompletionData> QueryOnLeft(IEnumerable<Token> tokens, string inputted)
        {
            var t = tokens.Memoize();
            if (!t.Any() || t.Last().Type == TokenType.Comma)
            {
                return new[]
                {
                    new CompletionData("local", QueryCompletionResources.SourceLocal),
                    new CompletionData("home", QueryCompletionResources.SourceHome),
                    new CompletionData("mention", QueryCompletionResources.SourceMention),
                    new CompletionData("messages", QueryCompletionResources.SourceMessages),
                    new CompletionData("list", "list:", QueryCompletionResources.SourceList),
                    new CompletionData("search", "search:", QueryCompletionResources.SourceSearch),
                    new CompletionData("track", "track:", QueryCompletionResources.SourceTrack),
                    new CompletionData("conv", "conv:", QueryCompletionResources.SourceConv),
                    new CompletionData("user", "user:", QueryCompletionResources.SourceUser)
                };
            }
            if (t.Any() &&
                (t.Last().Type == TokenType.Literal ||
                 t.Last().Type == TokenType.String) &&
                inputted == " ")
            {
                return new[] { new CompletionData("where", QueryCompletionResources.KeywordWhere) };
            }
            return null;
        }

        private IEnumerable<CompletionData> QueryOnRight(IEnumerable<Token> tokens, string inputted)
        {
            var accepts = " (.!" + Environment.NewLine;
            if (!accepts.Contains(inputted))
            {
                return null;
            }
            var lts = tokens.TakeLast(3).ToArray();
            Token first, second, third;
            switch (lts.Length)
            {
                case 0:
                    // first token of this query
                    return this.GetVariableCompletionData();
                case 1:
                    first = second = third = lts[0];
                    break;
                case 2:
                    first = second = lts[0];
                    third = lts[1];
                    break;
                case 3:
                    first = lts[0];
                    second = lts[1];
                    third = lts[2];
                    break;
                default:
                    return null;
            }
            if (lts.Length < 2 || third.Type != TokenType.Period)
            {
                return this.CheckPreviousIsVariable(third) ? null : this.GetVariableCompletionData();
            }
            if (second.IsMatchTokenLiteral("list") || first.Type == TokenType.Period)
            {
                // if completing list.*, suppress auto-completion hint.
                return null;
            }
            if (second.IsMatchTokenLiteral("user") || second.IsMatchTokenLiteral("retweeter"))
            {
                return this.GetUserObjectFieldCompletionData();
            }
            return this.GetAccountObjectFieldCompletionData();
        }

        private bool CheckPreviousIsVariable(Token token)
        {
            if (token.Type == TokenType.CloseParenthesis) return true;
            if (token.Type == TokenType.String) return false;
            if (token.Type == TokenType.Literal)
            {
                if (String.IsNullOrEmpty(token.Value))
                {
                    return true;
                }
                #region determine defined variables
                switch (token.Value.ToLower())
                {
                    case "we":
                    case "our":
                    case "us":
                    case "@":
                    case "block":
                    case "blocks":
                    case "blocking":
                    case "blockings":
                    case "user":
                    case "retweeter":
                    case "protected":
                    case "isprotected":
                    case "is_protected":
                    case "verified":
                    case "isverified":
                    case "is_verified":
                    case "translator":
                    case "istranslator":
                    case "is_translator":
                    case "contributorsenabled":
                    case "contributors_enabled":
                    case "iscontributorsenabled":
                    case "is_contributors_enabled":
                    case "geoenabled":
                    case "geo_enabled":
                    case "isgeoenabled":
                    case "is_geo_enabled":
                    case "id":
                    case "status":
                    case "statuses":
                    case "statuscount":
                    case "status_count":
                    case "statusescount":
                    case "statuses_count":
                    case "friend":
                    case "friends":
                    case "following":
                    case "followings":
                    case "friendscount":
                    case "friends_count":
                    case "followingscount":
                    case "followings_count":
                    case "follower":
                    case "followers":
                    case "followerscount":
                    case "followers_count":
                    case "fav":
                    case "favs":
                    case "favorite":
                    case "favorites":
                    case "favscount":
                    case "favs_count":
                    case "favoritescount":
                    case "favorites_count":
                    case "list":
                    case "listed":
                    case "listcount":
                    case "list_count":
                    case "listedcount":
                    case "listed_count":
                    case "screenname":
                    case "screen_name":
                    case "name":
                    case "username":
                    case "bio":
                    case "desc":
                    case "description":
                    case "loc":
                    case "location":
                    case "lang":
                    case "language":
                    case "dm":
                    case "isdm":
                    case "is_dm":
                    case "message":
                    case "ismessage":
                    case "is_message":
                    case "directmessage":
                    case "direct_message":
                    case "isdirectmessage":
                    case "is_direct_message":
                    case "rt":
                    case "retweet":
                    case "isretweet":
                    case "is_retweet":
                    case "replyto":
                    case "reply_to":
                    case "inreplyto":
                    case "in_reply_to":
                    case "mention":
                    case "to":
                    case "favorer":
                    case "favorers":
                    case "rts":
                    case "retweets":
                    case "retweeters":
                    case "text":
                    case "body":
                    case "via":
                    case "from":
                    case "source":
                    case "client":
                        return true;
                }
                #endregion
                switch (token.Value[0])
                {
                    case '@':
                    case '#':
                        return true;
                }
                long n;
                return Int64.TryParse(token.Value, out n);
            }
            return false;
        }

        private IEnumerable<CompletionData> GetVariableCompletionData()
        {
            return new[]
            {
                new CompletionData("us", QueryCompletionResources.VariableUser),
                new CompletionData("user", QueryCompletionResources.VariableUser),
                new CompletionData("retweeter", QueryCompletionResources.VariableRetweeter),
                new CompletionData("direct_message", QueryCompletionResources.VariableDirectMessage),
                new CompletionData("retweet", QueryCompletionResources.VariableRetweet),
                new CompletionData("id", QueryCompletionResources.VariableId),
                new CompletionData("in_reply_to", QueryCompletionResources.VariableInReplyTo),
                new CompletionData("body", QueryCompletionResources.VariableBody),
                new CompletionData("text", QueryCompletionResources.VariableBody),
                new CompletionData("via", QueryCompletionResources.VariableVia),
                new CompletionData("from", QueryCompletionResources.VariableVia),
                new CompletionData("to", QueryCompletionResources.VariableTo),
                new CompletionData("favs", QueryCompletionResources.VariableFavorites),
                new CompletionData("rts", QueryCompletionResources.VariableRetweets),
                new CompletionData("list", QueryCompletionResources.VariableList),
                new CompletionData("has_media", QueryCompletionResources.VariableHasMedia),
            };
        }

        private IEnumerable<CompletionData> GetAccountObjectFieldCompletionData()
        {
            return new[]
            {
                new CompletionData("followings", QueryCompletionResources.AcccountObjectFollowings),
                new CompletionData("followers", QueryCompletionResources.AccountObjectFollowers),
                new CompletionData("blockings", QueryCompletionResources.AccountObjectBlockings)
            };
        }

        private IEnumerable<CompletionData> GetUserObjectFieldCompletionData()
        {
            return new[]
            {
                new CompletionData("protected", QueryCompletionResources.UserObjectProtected),
                new CompletionData("verified", QueryCompletionResources.UserObjectVerified),
                new CompletionData("translator", QueryCompletionResources.UserObjectTranslator),
                new CompletionData("contributors_enabled", QueryCompletionResources.UserObjectContributorsEnabled),
                new CompletionData("geo_enabled", QueryCompletionResources.UserObjectGeoEnabled),
                new CompletionData("id", QueryCompletionResources.UserObjectId),
                new CompletionData("statuses", QueryCompletionResources.UserObjectStatuses),
                new CompletionData("followings", QueryCompletionResources.UserObjectFollowings),
                new CompletionData("followers", QueryCompletionResources.UserObjectFollowers),
                new CompletionData("favs", QueryCompletionResources.UserObjectFavorites),
                new CompletionData("listed_count", QueryCompletionResources.UserObjectListedCount),
                new CompletionData("screen_name", QueryCompletionResources.UserObjectScreenName),
                new CompletionData("name", QueryCompletionResources.UserObjectName),
                new CompletionData("bio", QueryCompletionResources.UserObjectBio),
                new CompletionData("loc", QueryCompletionResources.UserObjectLocation),
                new CompletionData("lang", QueryCompletionResources.UserObjectLanguage)
            };
        }

        #endregion
    }
}
