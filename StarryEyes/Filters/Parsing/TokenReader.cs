using System;
using System.Collections.Generic;
using System.Linq;

namespace StarryEyes.Filters.Parsing
{
    internal class TokenReader
    {
        List<Token> tokenQueueList;
        int queueCursor = 0;
        public TokenReader(IEnumerable<Token> tokens)
        {
            System.Diagnostics.Debug.WriteLine("Tokens: " + tokens.Select(t => t.ToString()).JoinString(Environment.NewLine));
            tokenQueueList = new List<Token>(tokens);
        }

        /// <summary>
        /// Get value and forward queue.
        /// </summary>
        /// <returns></returns>
        public Token Get()
        {
            System.Diagnostics.Debug.WriteLine("read:" + tokenQueueList[queueCursor]);
            return tokenQueueList[queueCursor++];
        }

        /// <summary>
        /// Look ahead next token.
        /// </summary>
        public Token LookAhead()
        {
            if (!IsRemainToken)
                throw new FilterQueryException("Query is terminated in halfway.", RemainQuery);
            return tokenQueueList[queueCursor];
        }

        /// <summary>
        /// Rewind queue.
        /// </summary>
        public void RewindOne()
        {
            if (queueCursor == 0)
                throw new InvalidOperationException("トークンリーダーは初期状態まで巻き戻っています。もう戻せません。");
            queueCursor--;
        }

        /// <summary>
        /// Check token remains in queue.
        /// </summary>
        public bool IsRemainToken
        {
            get { return queueCursor < tokenQueueList.Count; }
        }

        public string RemainQuery
        {
            get
            {
                return tokenQueueList
                    .Skip(queueCursor)
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
                throw new FilterQueryException("Invalid token: " + token + " in " + token.DebugIndex + ", expected is: " + type, RemainQuery);
            return token;
        }

        private void RaiseQueryInvalidTerminatedError(TokenType expected, string innerQuery)
        {
            throw new FilterQueryException("Query is terminated in halfway. Next token is expected: " + expected.ToString(), innerQuery);
        }
    }
}
