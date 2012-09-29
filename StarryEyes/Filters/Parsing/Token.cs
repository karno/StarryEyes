
namespace StarryEyes.Filters.Parsing
{
    /// <summary>
    /// Represents token.
    /// </summary>
    internal struct Token
    {
        /// <summary>
        /// このトークンの種別
        /// </summary>
        public TokenType Type { get; set; }

        /// <summary>
        /// このトークンの値
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// デバッグ用のインデックス
        /// </summary>
        public int DebugIndex { get; set; }

        public Token(TokenType type, int debugIndex)
            : this()
        {
            this.Type = type;
            this.Value = null;
            this.DebugIndex = debugIndex;
        }

        public Token(TokenType type, string value, int debugIndex)
            : this()
        {
            this.Type = type;
            this.Value = value;
            this.DebugIndex = debugIndex;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case TokenType.OpenBracket:
                    return "( [開き括弧]";
                case TokenType.CloseBracket:
                    return ") [閉じ括弧]";
                case TokenType.OperatorPlus:
                    return "+ [和]";
                case TokenType.OperatorMinus:
                    return "- [差]";
                case TokenType.OperatorMultiple:
                    return "* [積]";
                case TokenType.OperatorDivide:
                    return "/ [商]";
                case TokenType.OperatorAnd:
                    return "&& [論理積]";
                case TokenType.OperatorOr:
                    return "|| [論理和]";
                case TokenType.OperatorContains:
                    return "-> [含む]";
                case TokenType.OperatorContainedBy:
                    return "<- [含まれる]";
                case TokenType.OperatorEquals:
                    return "== [等しい]";
                case TokenType.OperatorNotEquals:
                    return "!= [等しくない]";
                case TokenType.OperatorLessThan:
                    return "< [より小さい]";
                case TokenType.OperatorLessThanOrEqual:
                    return "<= [より小さいか等しい]";
                case TokenType.OperatorGreaterThan:
                    return "> [より大きい]";
                case TokenType.OperatorGreaterThanOrEqual:
                    return ">= [より大きいか等しい]";
                case TokenType.Literal:
                    return "リテラル (" + (Value == null ? "[null]" : Value) + ")";
                case TokenType.Period:
                    return ". [ピリオド]";
                case TokenType.Comma:
                    return ", [カンマ]";
                case TokenType.Collon:
                    return ": [コロン]";
                case TokenType.Exclamation:
                    return "! [エクスクラメーション]";
                case TokenType.String:
                    return "文字列 (" + (Value == null ? "[null]" : "\"" + Value + "\"") + ")";
                default:
                    return "[不明なトークン(" + Value + ")]";

            }
        }
    }

    /// <summary>
    /// Types of token.
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// Period
        /// </summary>
        Period,
        /// <summary>
        /// Comma
        /// </summary>
        Comma,
        /// <summary>
        /// Collon
        /// </summary>
        Collon,
        /// <summary>
        /// Exclamation
        /// </summary>
        Exclamation,
        /// <summary>
        /// Token Literal
        /// </summary>
        Literal,
        /// <summary>
        /// Operator plus, +
        /// </summary>
        OperatorPlus,
        /// <summary>
        /// Operator minus, -
        /// </summary>
        OperatorMinus,
        /// <summary>
        /// Operator multiple, *(Asterisk)
        /// </summary>
        OperatorMultiple,
        /// <summary>
        /// Operator divide, /
        /// </summary>
        OperatorDivide,
        /// <summary>
        /// Operator logical and, &amp;&amp;
        /// </summary>
        OperatorAnd,
        /// <summary>
        /// Operator logical or, ||
        /// </summary>
        OperatorOr,
        /// <summary>
        /// Set contains, -&gt;
        /// </summary>
        OperatorContains,
        /// <summary>
        /// Set contained in, &lt;-
        /// </summary>
        OperatorContainedBy,
        /// <summary>
        /// Equals, ==
        /// </summary>
        OperatorEquals,
        /// <summary>
        /// Not equals, !=
        /// </summary>
        OperatorNotEquals,
        /// <summary>
        /// Less than, &lt; 
        /// </summary>
        OperatorLessThan,
        /// <summary>
        /// Less than or equal, &lt;=
        /// </summary>
        OperatorLessThanOrEqual,
        /// <summary>
        /// Greater than, &gt;
        /// </summary>
        OperatorGreaterThan,
        /// <summary>
        /// Greater than or equal, &gt;= 
        /// </summary>
        OperatorGreaterThanOrEqual,
        /// <summary>
        /// Open bracket, (
        /// </summary>
        OpenBracket,
        /// <summary>
        /// Close bracket, )
        /// </summary>
        CloseBracket,
        /// <summary>
        /// Quoted string
        /// </summary>
        String,
    }
}
