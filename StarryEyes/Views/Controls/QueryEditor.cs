using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using StarryEyes.Filters.Parsing;
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
            DependencyProperty.Register("IsSourceFilterEditable", typeof(bool), typeof(QueryEditor), new PropertyMetadata(null));

        public QueryEditor()
            : base()
        {
            this.ShowLineNumbers = true;
            this.LoadXshd();
            this.TextArea.TextEntering += TextArea_TextEntering;
            this.TextArea.TextEntered += TextArea_TextEntered;
        }

        private const string resourceName = "StarryEyes.Views.Controls.QueryEditorResources.KrileQuery.xshd";

        private void LoadXshd()
        {
            var asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream(resourceName))
            using (var reader = XmlReader.Create(stream))
            {
                this.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }

        protected override void OnTextChanged(System.EventArgs e)
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
                if (this._completionWindow == wnd)
                    this._completionWindow = null;
            };
            var elems = wnd.CompletionList.CompletionData;
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
                    return new[] { new CompletionData("from", "rom", "from につづいて、ソースクエリを指定できます。") };
                case "w":
                    return new[] { new CompletionData("where", "here", "where につづいて、フィルタクエリを指定できます。") };
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
		    new CompletionData("local","Krile内のすべてのツイート(引数: なし)"), 
		    new CompletionData("home", "ユーザのホームタイムライン(引数: ユーザースクリーン名[省略可能])"), 
		    new CompletionData("mention", "返信タイムライン(引数: ユーザースクリーン名[省略可能])"), 
		    new CompletionData("messages", "ダイレクトメッセージ(引数: ユーザースクリーン名[省略可能])"), 
		    new CompletionData("list", "list:", "リスト(引数: (取得ユーザ名)/スクリーン名/リスト名)"), 
		    new CompletionData("search", "search:", "検索タイムライン(引数: 検索文字列)"), 
		    new CompletionData("track", "track:", "ストリームタイムライン(引数: 検索文字列[英数字])"), 
		    new CompletionData("conv", "conv:", "返信タイムライン(引数: ツイートID)"), 
                };
            }
            if (t.Any() &&
                (t.Last().Type == TokenType.Literal ||
                 t.Last().Type == TokenType.String) &&
                inputted == " ")
            {
                return new[] { new CompletionData("where", "where につづいて、フィルタクエリを指定できます。") };
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
            var lts = tokens.TakeLast(2).ToArray();
            if (lts.Length == 0)
            {
                // first
                return this.GetVariableCompletionData();
            }
            var lt = lts.LastOrDefault();
            var plt = lts.FirstOrDefault();
            if (lts.Length >= 2 && lt.Type == TokenType.Period)
            {
                if (plt.IsMatchTokenLiteral("user") || plt.IsMatchTokenLiteral("retweeter"))
                {
                    return this.GetUserObjectFieldCompletionData();
                }
                return this.GetAccountObjectFieldCompletionData();
            }
            return this.CheckPreviousIsVariable(lt) ? null : this.GetVariableCompletionData();
        }

        private bool CheckPreviousIsVariable(Token token)
        {
            if (token.Type == TokenType.CloseBracket) return true;
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
		new CompletionData("us", "[Account] Krileに登録済みのアカウント一覧"), 
		new CompletionData("user", "[User] ツイートのユーザー"), 
		new CompletionData("retweeter", "[User] リツイートしたユーザー"), 
		new CompletionData("direct_message", "[Boolean] ダイレクトメッセージであるか"), 
		new CompletionData("retweet", "[Boolean] リツイートであるか"), 
		new CompletionData("id", "[Numeric] ツイートのID"), 
		new CompletionData("in_reply_to", "[Numeric] 返信先ツイートID"),
		new CompletionData("text", "[String] ツイートの本文"),
		new CompletionData("body", "[String] ツイートの本文"),
		new CompletionData("via", "[String] ツイートの送信元クライアント"),
		new CompletionData("from", "[String] ツイートの送信元クライアント"),
		new CompletionData("to", "[Num/Str/Set] ツイートの返信先ユーザー"),
		new CompletionData("favs", "[Num/Set] 被お気に入り登録数"),
		new CompletionData("rts", "[Num/Set] 被リツイート数"),
            };
        }

        private IEnumerable<CompletionData> GetAccountObjectFieldCompletionData()
        {
            return new[]
            {
		new CompletionData("followings", "[Set] フォローしているユーザー"), 
		new CompletionData("followers", "[Set] フォローされているユーザー"), 
		new CompletionData("blockings", "[Set] ブロックしているユーザー"), 
            };
        }

        private IEnumerable<CompletionData> GetUserObjectFieldCompletionData()
        {
            return new[]
            {
                new CompletionData("protected", "[Boolean] ツイートを非公開にしているか"),
                new CompletionData("verified", "[Boolean] 公式認証済みであるか"),
                new CompletionData("translator", "[Boolean] 翻訳者であるか"),
                new CompletionData("contributors_enabled", "[Boolean] コントリビュータを有効にしているか"),
                new CompletionData("geo_enabled", "[Boolean] 位置情報を有効にしているか"),
                new CompletionData("id", "[Numeric] ユーザーID"),
                new CompletionData("statuses", "[Numeric] ツイート数"),
                new CompletionData("followings", "[Numeric] フォロー数"),
                new CompletionData("followers", "[Numeric] フォロー数"),
                new CompletionData("favs", "[Numeric] お気に入り登録数"),
                new CompletionData("listed_count", "[Numeric] リスト被登録"),
                new CompletionData("screen_name", "[String] スクリーン名(@ID)"),
                new CompletionData("name", "[String] ユーザー名"),
                new CompletionData("bio", "[String] プロフィール"),
                new CompletionData("loc", "[String] 所在地"),
                new CompletionData("lang", "[String] 言語"),
            };
        }

        #endregion
    }
}
