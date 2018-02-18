using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Globalization.Filters;

namespace StarryEyes.Filters.Parsing
{
    internal static class Tokenizer
    {
        public static IEnumerable<Token> Tokenize(string query, bool suppressErrors = false)
        {
            int strptr = 0;
            do
            {
                switch (query[strptr])
                {
                    case '&': // &, &&
                        if (CheckNext(query, strptr, '&'))
                        {
                            strptr++;
                        }
                        yield return new Token(TokenType.OperatorAnd, strptr);
                        break;
                    case '|':
                        if (CheckNext(query, strptr, '|'))
                        {
                            strptr++;
                        }
                        yield return new Token(TokenType.OperatorOr, strptr);
                        break;
                    case '<':
                        if (CheckNext(query, strptr, '='))
                        {
                            yield return new Token(TokenType.OperatorLessThanOrEqual, strptr);
                            strptr++;
                        }
                        else if (CheckNext(query, strptr, '-'))
                        {
                            yield return new Token(TokenType.OperatorIn, strptr);
                            strptr++;
                        }
                        else
                        {
                            yield return new Token(TokenType.OperatorLessThan, strptr);
                        }
                        break;
                    case '>':
                        if (CheckNext(query, strptr, '='))
                        {
                            yield return new Token(TokenType.OperatorGreaterThanOrEqual, strptr);
                            strptr++;
                        }
                        else
                        {
                            yield return new Token(TokenType.OperatorGreaterThan, strptr);
                        }
                        break;
                    case '(':
                        yield return new Token(TokenType.OpenParenthesis, strptr);
                        break;
                    case ')':
                        yield return new Token(TokenType.CloseParenthesis, strptr);
                        break;
                    case '[':
                        yield return new Token(TokenType.OpenSquareBracket, strptr);
                        break;
                    case ']':
                        yield return new Token(TokenType.CloseSquareBracket, strptr);
                        break;
                    case '+':
                        yield return new Token(TokenType.OperatorPlus, strptr);
                        break;
                    case '-':
                        if (CheckNext(query, strptr, '>'))
                        {
                            yield return new Token(TokenType.OperatorContains, strptr);
                            strptr++;
                        }
                        else
                        {
                            yield return new Token(TokenType.OperatorMinus, strptr);
                        }
                        break;
                    case '*':
                        yield return new Token(TokenType.OperatorMultiple, strptr);
                        break;
                    case '/':
                        yield return new Token(TokenType.OperatorDivide, strptr);
                        break;
                    case '.':
                        yield return new Token(TokenType.Period, strptr);
                        break;
                    case ',':
                        yield return new Token(TokenType.Comma, strptr);
                        break;
                    case ':':
                        yield return new Token(TokenType.Collon, strptr);
                        break;
                    case '=': // == or =
                        if (CheckNext(query, strptr, '='))
                        {
                            strptr++;
                        }
                        yield return new Token(TokenType.OperatorEquals, strptr);
                        break;
                    case '!':
                        if (CheckNext(query, strptr, '='))
                        {
                            yield return new Token(TokenType.OperatorNotEquals, strptr);
                            strptr++;
                        }
                        else
                        {
                            yield return new Token(TokenType.Exclamation, strptr);
                        }
                        break;
                    case '"':
                        yield return new Token(TokenType.String,
                                               GetInQuoteString(query, ref strptr, suppressErrors),
                                               strptr);
                        break;
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        // skip reading
                        break;
                    default:
                        var begin = strptr;
                        // fast forward to hit any token splitters
                        const string tokens = "&|<>()[]+-*/.,:=!\" \t\r\n";
                        do
                        {
                            if (tokens.Contains(query[strptr]))
                            {
                                // create literal
                                yield return new Token(TokenType.Literal,
                                    query.Substring(begin, strptr - begin), begin);
                                strptr--; // prepare to increment bottom (indicate correct point after incremented)
                                break;
                            }
                            strptr++;
                        } while (strptr < query.Length);
                        // token splitters is not contained
                        if (strptr == query.Length)
                        {
                            yield return new Token(TokenType.Literal,
                                query.Substring(begin, strptr - begin), begin);
                        }
                        break;
                }
                strptr++;
            } while (strptr < query.Length);
        }

        /// <summary>
        /// Gets text between quotes. Consider escape-sequences.
        /// </summary>
        /// <param name="query">query string</param>
        /// <param name="cursor">Starting index of quotes, after this method, returns last index of quote.</param>
        /// <param name="suppressErrors">suppress exceptions</param>
        /// <returns>string</returns>
        public static string GetInQuoteString(string query, ref int cursor, bool suppressErrors)
        {
            var begin = cursor++;
            while (cursor < query.Length)
            {
                if (query[cursor] == '\\')
                {
                    // skip next double quote or backslash
                    if (cursor + 1 == query.Length)
                    {
                        if (suppressErrors)
                        {
                            return query.Substring(begin + 1).UnescapeFromQuery();
                        }
                        throw new ArgumentException(QueryCompilerResources.QueryEndsWithBackslash);
                    }
                    if (query[cursor + 1] == '"' || query[cursor + 1] == '\\')
                    {
                        cursor++;
                    }
                }
                else if (query[cursor] == '"')
                {
                    // string is closed
                    return query.Substring(begin + 1, cursor - begin - 1).UnescapeFromQuery();
                }
                cursor++;
            }
            // end string forced
            if (suppressErrors)
            {
                return query.Substring(begin + 1).UnescapeFromQuery();
            }
            throw new ArgumentException(QueryCompilerResources.QueryStringIsNotClosed + " " + query.Substring(begin));
        }

        public static bool CheckNext(string q, int i, char c)
        {
            return q.Length > i + 1 && q[i + 1] == c;
        }
    }
}
