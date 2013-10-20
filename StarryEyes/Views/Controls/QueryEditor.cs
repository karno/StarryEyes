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
            asm.GetManifestResourceNames().ForEach(n => System.Diagnostics.Debug.WriteLine(n));
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
            switch (inputted)
            {
                case "F":
                case "f":
                    return new[] { new CompletionData("from", "rom", "from につづいて、ソースクエリを指定できます。") };
                case "W":
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
            return null;
        }

        private IEnumerable<CompletionData> QueryOnRight(IEnumerable<Token> tokens, string inputted)
        {
            return null;
        }


        #endregion

    }
}
