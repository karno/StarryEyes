
using System;
using System.Text;

namespace Detective
{
    public static class SelfAnalyzer
    {
        public static string AnalyzeResult { get; private set; }

        public static bool Analyze(string log)
        {
            var result = new StringBuilder();
            if (log.Contains("System.IO.FileLoadException", StringComparison.CurrentCultureIgnoreCase))
            {
                result.AppendLine("必須ライブラリの読み込みに失敗しました。");
                result.AppendLine("このエラーの原因としては、以下の問題が考えられます。");
                result.AppendLine();
                result.AppendLine("・サポートされていないプラットフォームで実行しようとしている");
                result.AppendLine("　サポートされているプラットフォームは、Windows Vista以降です。");
                result.AppendLine("　64bit環境でも問題なく動作することを確認しています。");
                result.AppendLine("　バーチャルマシンなどでの環境では正しく動作しないことがあります。");
                result.AppendLine();
                result.AppendLine("・Krileの起動に必要な権限が不足している");
                result.AppendLine("　Krileを管理者権限で起動し、問題が改善されるか確認してください。");
                result.AppendLine();
                result.AppendLine("・.NET Framework 4.5 のインストールが正しく行われていない");
                result.AppendLine("　.NET Framework 4.5 の再インストールを試してみてください。");
                result.AppendLine("----------");
            }

            if (log.Contains("System.Data.SQLite.SQLiteException", StringComparison.CurrentCultureIgnoreCase) &&
                log.Contains("database disk image is malformed", StringComparison.CurrentCultureIgnoreCase))
            {
                result.AppendLine("SQLite データベースが破損しています。");
                result.AppendLine("このまま Krile をリスタートすると、データベースの再構築を行うことができます。");
                result.AppendLine();
                result.AppendLine("リスタートしてもデータベースのクリアが提案されない場合は、メンテナンスモードでのKrileの起動を試みるか、手動でデータベースを削除してください。");
                result.AppendLine("メンテナンスモードでの起動方法やデータベースファイルの場所については、公式サイトのFAQを参照してください。");
                result.AppendLine("----------");
            }
            AnalyzeResult = result.ToString();
            return AnalyzeResult.Length > 0;
        }

        private static bool Contains(this string haystack, string neeedle, StringComparison comparison)
        {
            return haystack.IndexOf(neeedle, comparison) >= 0;
        }
    }
}
