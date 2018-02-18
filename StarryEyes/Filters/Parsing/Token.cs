
using StarryEyes.Globalization.Filters;

namespace StarryEyes.Filters.Parsing
{
    /// <summary>
    /// Represents token.
    /// </summary>
    internal struct Token
    {
        /// <summary>
        /// Type of token
        /// </summary>
        public TokenType Type { get; set; }

        /// <summary>
        /// Value of token
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Index for debugging
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
                case TokenType.OpenParenthesis:
                    return "( [" + QueryCompilerResources.TokenOpenParenthesis + "]";
                case TokenType.CloseParenthesis:
                    return ") [" + QueryCompilerResources.TokenCloseParenthesis + "]";
                case TokenType.OpenSquareBracket:
                    return "[ [" + QueryCompilerResources.TokenOpenSquareBracket + "]";
                case TokenType.CloseSquareBracket:
                    return "] [" + QueryCompilerResources.TokenCloseSquareBracket + "]";
                case TokenType.OperatorPlus:
                    return "+ [" + QueryCompilerResources.TokenOperatorPlus + "]";
                case TokenType.OperatorMinus:
                    return "- [" + QueryCompilerResources.TokenOperatorMinus + "]";
                case TokenType.OperatorMultiple:
                    return "* [" + QueryCompilerResources.TokenOperatorMultiple + "]";
                case TokenType.OperatorDivide:
                    return "/ [" + QueryCompilerResources.TokenOperatorDivide + "]";
                case TokenType.OperatorAnd:
                    return "&& [" + QueryCompilerResources.TokenOperatorAnd + "]";
                case TokenType.OperatorOr:
                    return "|| [" + QueryCompilerResources.TokenOperatorOr + "]";
                case TokenType.OperatorContains:
                    return "-> [" + QueryCompilerResources.TokenOperatorContains + "]";
                case TokenType.OperatorIn:
                    return "<- [" + QueryCompilerResources.TokenOperatorIn + "]";
                case TokenType.OperatorEquals:
                    return "== [" + QueryCompilerResources.TokenOperatorEquals + "]";
                case TokenType.OperatorNotEquals:
                    return "!= [" + QueryCompilerResources.TokenOperatorNotEquals + "]";
                case TokenType.OperatorLessThan:
                    return "< [" + QueryCompilerResources.TokenOperatorLessThan + "]";
                case TokenType.OperatorLessThanOrEqual:
                    return "<= [" + QueryCompilerResources.TokenOperatorLessThanOrEqual + "]";
                case TokenType.OperatorGreaterThan:
                    return "> [" + QueryCompilerResources.TokenOperatorGreaterThan + "]";
                case TokenType.OperatorGreaterThanOrEqual:
                    return ">= [" + QueryCompilerResources.TokenOperatorGreaterThanOrEqual + "]";
                case TokenType.Literal:
                    return "[" + QueryCompilerResources.TokenLiteral + " (" + (Value ?? "[null]") + ")]";
                case TokenType.Period:
                    return ". [" + QueryCompilerResources.TokenPeriod + "]";
                case TokenType.Comma:
                    return ", [" + QueryCompilerResources.TokenComma + "]";
                case TokenType.Collon:
                    return ": [" + QueryCompilerResources.TokenCollon + "]";
                case TokenType.Exclamation:
                    return "! [" + QueryCompilerResources.TokenExclamation + "]";
                case TokenType.String:
                    return "[" + QueryCompilerResources.TokenString + " (" + (Value == null ? "[null]" : Value.Quote()) + ")]";
                default:
                    return "[" + QueryCompilerResources.TokenUnknown + " (" + Value + ")]";

            }
        }
    }

    internal static class TokenExtensions
    {
        internal static bool IsMatchTokenLiteral(this Token token, string value, bool ignoreCase = true)
        {
            if (token.Type != TokenType.Literal) return false;
            return ignoreCase ? token.Value.ToUpper() == value.ToUpper() : token.Value == value;
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
        OperatorIn,
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
        OpenParenthesis,
        /// <summary>
        /// Close bracket, )
        /// </summary>
        CloseParenthesis,
        /// <summary>
        /// Open square bracket, (
        /// </summary>
        OpenSquareBracket,
        /// <summary>
        /// Close square bracket, ]
        /// </summary>
        CloseSquareBracket,
        /// <summary>
        /// Quoted string
        /// </summary>
        String,
    }
}
