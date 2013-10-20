using System;
using System.Collections.Generic;
using System.Linq;

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
                        yield return new Token(TokenType.OpenBracket, strptr);
                        break;
                    case ')':
                        yield return new Token(TokenType.CloseBracket, strptr);
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
                        // 何らかのトークン分割子に出会うまで空回し
                        const string tokens = "&|<>()+-*/.,:=!\" \t\r\n";
                        do
                        {
                            if (tokens.Contains(query[strptr]))
                            {
                                // リテラル生成
                                yield return new Token(TokenType.Literal,
                                    query.Substring(begin, strptr - begin), begin);
                                strptr--; // 巻き戻し
                                break;
                            }
                            strptr++;
                        } while (strptr < query.Length);
                        // トークン分割子に出会わなかった場合
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
                    // 次のダブルクオートかバックスラッシュを読み飛ばす
                    if (cursor + 1 == query.Length)
                    {
                        if (suppressErrors)
                        {
                            return query.Substring(begin + 1).UnescapeFromQuery();
                        }
                        throw new ArgumentException("クエリはバックスラッシュで終了しています。");
                    }
                    if (query[cursor + 1] == '"' || query[cursor + 1] == '\\')
                    {
                        cursor++;
                    }
                }
                else if (query[cursor] == '"')
                {
                    // ここで文字列おしまい
                    return query.Substring(begin + 1, cursor - begin - 1).UnescapeFromQuery();
                }
                cursor++;
            }
            // 文字列を無理やり終える
            if (suppressErrors)
            {
                return query.Substring(begin + 1).UnescapeFromQuery();
            }
            throw new ArgumentException("文字列が閉じられていません: " + query.Substring(begin));
        }

        public static bool CheckNext(string q, int i, char c)
        {
            return q.Length > i + 1 && q[i + 1] == c;
        }
    }
}
