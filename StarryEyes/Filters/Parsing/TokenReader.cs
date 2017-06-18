using System.Collections.Generic;
using System.Linq;
using StarryEyes.Globalization.Filters;

namespace StarryEyes.Filters.Parsing
{
    internal class TokenReader
    {
        readonly List<Token> _tokenQueueList;
        int _queueCursor;
        public TokenReader(IEnumerable<Token> tokens)
        {
            _tokenQueueList = new List<Token>(tokens);
        }

        /// <summary>
        /// Get value and forward queue.
        /// </summary>
        /// <returns></returns>
        public Token Get()
        {
            return _tokenQueueList[_queueCursor++];
        }

        /// <summary>
        /// Look ahead next token.
        /// </summary>
        public Token LookAhead()
        {
            if (!IsRemainToken)
                throw new FilterQueryException(QueryCompilerResources.QueryInterrupted, RemainQuery);
            return _tokenQueueList[_queueCursor];
        }

        /// <summary>
        /// Check token remains in queue.
        /// </summary>
        public bool IsRemainToken
        {
            get { return _queueCursor < _tokenQueueList.Count; }
        }

        public string RemainQuery
        {
            get
            {
                return _tokenQueueList
                    .Skip(_queueCursor)
                    .Select(t => t.Value)
                    .JoinString(" ");
            }
        }

        /// <summary>
        /// Get value with assertion.
        /// </summary>
        /// <param name="type">supposed token type</param>
        /// <returns>token</returns>
        public Token AssertGet(TokenType type)
        {
            if (!this.IsRemainToken)
                RaiseQueryInvalidTerminatedError(type, RemainQuery);
            var token = Get();
            if (token.Type != type)
                throw QueryCompiler.CreateUnexpectedTokenError(token + " in " + token.DebugIndex, RemainQuery);
            return token;
        }

        private void RaiseQueryInvalidTerminatedError(TokenType expected, string innerQuery)
        {
            throw new FilterQueryException(
                QueryCompilerResources.QueryInterrupted +
                QueryCompilerResources.QueryPredictNextToken + " " + expected, innerQuery);
        }
    }
}
