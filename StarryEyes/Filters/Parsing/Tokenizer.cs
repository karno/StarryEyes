using System;
using System.Collections.Generic;
using System.Linq;

namespace StarryEyes.Filters.Parsing
{
    internal static class Tokenizer
    {
        public static IEnumerable<Token> Tokenize(string query)
        {
            int strptr = 0;
            do
            {
                switch (query[strptr])
                {
                    case '&':
                        yield return new Token(TokenType.OperatorAnd, strptr);
                        break;
                    case '|':
                        yield return new Token(TokenType.OperatorOr, strptr);
                        break;
                    case '<':
                        if (query.Length <= strptr + 1)
                            throw new ArgumentException("クエリが短すぎます。");
                        switch (query[strptr + 1])
                        {
                            case '=': // <=
                                yield return new Token(TokenType.OperatorLessThanOrEqual, strptr);
                                strptr++;
                                break;
                            case '-': // <-
                                yield return new Token(TokenType.OperatorContainedBy, strptr);
                                strptr++;
                                break;
                            default: // <
                                yield return new Token(TokenType.OperatorLessThan, strptr);
                                break;
                        }
                        break;
                    case '>':
                        if (query.Length <= strptr + 1)
                            throw new ArgumentException("クエリが短すぎます。");
                        switch (query[strptr + 1])
                        {
                            case '=': // >=
                                yield return new Token(TokenType.OperatorGreaterThanOrEqual, strptr);
                                strptr++;
                                break;
                            default: // >
                                yield return new Token(TokenType.OperatorGreaterThan, strptr);
                                break;
                        }
                        break;
                    case '+':
                        yield return new Token(TokenType.OperatorPlus, strptr);
                        break;
                    case '-':
                        if (query.Length <= strptr + 1)
                            throw new ArgumentException("クエリが短すぎます。");
                        switch (query[strptr + 1])
                        {
                            case '>': // ->
                                yield return new Token(TokenType.OperatorContains, strptr);
                                strptr++;
                                break;
                            default:
                                yield return new Token(TokenType.OperatorMinus, strptr);
                                break;
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
                    case '=':
                        AssertNext(query, strptr, '=');
                        yield return new Token(TokenType.OperatorEquals, strptr);
                        strptr++;
                        break;
                    case '!':
                        if (query.Length <= strptr + 1)
                            throw new ArgumentException("クエリが短すぎます。");
                        switch (query[strptr + 1])
                        {
                            case '=': // !=
                                yield return new Token(TokenType.OperatorNotEquals, strptr);
                                strptr++;
                                break;
                            default:
                                yield return new Token(TokenType.Exclamation, strptr);
                                break;
                        }
                        break;
                    case '(':
                        yield return new Token(TokenType.OpenBracket, strptr);
                        break;
                    case ')':
                        yield return new Token(TokenType.CloseBracket, strptr);
                        break;
                    case '"':
                        yield return new Token(TokenType.String, GetInQuoteString(query, ref strptr), strptr);
                        break;
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        // skip reading
                        break;
                    default:
                        int begin = strptr;
                        // 何らかのトークン分割子に出会うまで空回し
                        const string tokens = "&|.,:!()\"= \t\r\n";
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
        /// 先頭から終了のダブルクオートに出会うまでのテキストを取得します。<para />
        /// エスケープシーケンスを考慮します。
        /// </summary>
        /// <param name="query">クエリ文字列</param>
        /// <param name="cursor">文字列の開始ダブルクオートのインデックスを渡してください。解析終了後は、文字列終了のダブルクオートを示します</param>
        /// <returns>文字列部分</returns>
        /// <exception cref="System.ArgumentException">文字列の解析に失敗</exception>
        public static string GetInQuoteString(string query, ref int cursor)
        {
            int begin = cursor++;
            while (cursor < query.Length)
            {
                if (query[cursor] == '\\')
                {
                    // 次のダブルクオートかバックスラッシュを読み飛ばす
                    if (cursor + 1 == query.Length)
                    {
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
            throw new ArgumentException("文字列が閉じられていません: " + query.Substring(begin));
        }

        public static bool CheckNext(string q, int i, char c)
        {
            if (i + 1 >= q.Length || q[i] != c)
                return false;
            return true;
        }

        public static void AssertNext(string q, int i, char c)
        {
            if (i + 1 >= q.Length)
                throw new ArgumentException("クエリが短すぎます。");
            if (q[i] != c)
                throw new ArgumentException("この文字は予期されていません: " + q[i] + "(in \"" + q + "\" , index " + i + "), 予期されているもの: " + c);
        }
    }
}
