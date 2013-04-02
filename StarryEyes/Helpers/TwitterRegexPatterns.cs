using System.Text.RegularExpressions;

namespace StarryEyes.Helpers
{
    public static class TwitterRegexPatterns
    {
        private const string AlnumChars = "a-zA-Z0-9";
        private const string PunctChars = @"\p{P}\p{S}";

        private const string UnicodeSpaces =
            "[" + "\u0009-\u000d" + //  # White_Space # Cc   [5] <control-0009>..<control-000D>
            "\u0020" + // White_Space # Zs       SPACE
            "\u0085" + // White_Space # Cc       <control-0085>
            "\u00a0" + // White_Space # Zs       NO-BREAK SPACE
            "\u1680" + // White_Space # Zs       OGHAM SPACE MARK
            "\u180E" + // White_Space # Zs       MONGOLIAN VOWEL SEPARATOR
            "\u2000-\u200a" + // # White_Space # Zs  [11] EN QUAD..HAIR SPACE
            "\u2028" + // White_Space # Zl       LINE SEPARATOR
            "\u2029" + // White_Space # Zp       PARAGRAPH SEPARATOR
            "\u202F" + // White_Space # Zs       NARROW NO-BREAK SPACE
            "\u205F" + // White_Space # Zs       MEDIUM MATHEMATICAL SPACE
            "\u3000" + // White_Space # Zs       IDEOGRAPHIC SPACE
            "]";

        private const string LatinAccentsChars =
            "\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff" + // Latin-1
            "\u0100-\u024f" + // Latin Extended A and B
            "\u0253\u0254\u0256\u0257\u0259\u025b\u0263\u0268\u026f\u0272\u0289\u028b" + // IPA Extensions
            "\u02bb" + // Hawaiian
            "\u0300-\u036f" + // Combining diacritics
            "\u1e00-\u1eff"; // Latin Extended Additional (mostly for Vietnamese)

        private const string HashtagAlphaChars =
            "a-z" + LatinAccentsChars + "\u0400-\u04ff\u0500-\u0527" + // Cyrillic
            "\u2de0-\u2dff\ua640-\ua69f" + // Cyrillic Extended A/B
            "\u0591-\u05bf\u05c1-\u05c2\u05c4-\u05c5\u05c7" + "\u05d0-\u05ea\u05f0-\u05f4" + // Hebrew
            "\ufb1d-\ufb28\ufb2a-\ufb36\ufb38-\ufb3c\ufb3e\ufb40-\ufb41" + "\ufb43-\ufb44\ufb46-\ufb4f" +
            // Hebrew Pres. Forms
            "\u0610-\u061a\u0620-\u065f\u066e-\u06d3\u06d5-\u06dc" +
            "\u06de-\u06e8\u06ea-\u06ef\u06fa-\u06fc\u06ff" + // Arabic
            "\u0750-\u077f\u08a0\u08a2-\u08ac\u08e4-\u08fe" + // Arabic Supplement and Extended A
            "\ufb50-\ufbb1\ufbd3-\ufd3d\ufd50-\ufd8f\ufd92-\ufdc7\ufdf0-\ufdfb" + // Pres. Forms A
            "\ufe70-\ufe74\ufe76-\ufefc" + // Pres. Forms B
            "\u200c" + // Zero-Width Non-Joiner
            "\u0e01-\u0e3a\u0e40-\u0e4e" + // Thai
            "\u1100-\u11ff\u3130-\u3185\uA960-\uA97F\uAC00-\uD7AF\uD7B0-\uD7FF" + // Hangul (Korean)
            "\\p{IsHiragana}\\p{IsKatakana}" + // Japanese Hiragana and Katakana
            "\\p{IsCJKUnifiedIdeographs}" + // Japanese Kanji / Chinese Han
            "\u3003\u3005\u303b" + // Kanji/Han iteration marks
            "\uff21-\uff3a\uff41-\uff5a" + // full width Alphabet
            "\uff66-\uff9f" + // half width Katakana
            "\uffa1-\uffdc"; // half width Hangul (Korean)

        private const string HashtagAlphaNumericChars = "0-9\uff10-\uff19_" + HashtagAlphaChars;
        private const string HashtagAlpha = "[" + HashtagAlphaChars + "]";
        private const string HashtagAlphaNumeric = "[" + HashtagAlphaNumericChars + "]";

        /* URL related hash regex collection */
        private const string UrlValidPreceeding = "(?:[^A-Z0-9@＠$#＃\u202A-\u202E]|^)";

        private const string UrlValidChars = AlnumChars + LatinAccentsChars;
        private const string UrlValidSubdomain = "(?:(?:[" + UrlValidChars + "][" + UrlValidChars + "\\-_]*)?[" + UrlValidChars + "]\\.)";
        private const string UrlValidDomainName = "(?:(?:[" + UrlValidChars + "][" + UrlValidChars + "\\-]*)?[" + UrlValidChars + "]\\.)";
        /* Any non-space, non-punctuation characters. \p{Z} = any kind of whitespace or invisible separator. */
        private const string UrlValidUnicode = "[^" + PunctChars + "\\s\\p{Z}\\p{IsGeneralPunctuation}]";

        private const string UrlValidGtld =
            "(?:(?:aero|asia|biz|cat|com|coop|edu|gov|info|int|jobs|mil|mobi|museum|" +
            "name|net|org|pro|tel|travel|xxx)(?=[^" + AlnumChars + "]|$))";

        private const string UrlValidCctld =
            "(?:(?:ac|ad|ae|af|ag|ai|al|am|an|ao|aq|ar|as|at|au|aw|ax|az|" +
            "ba|bb|bd|be|bf|bg|bh|bi|bj|bm|bn|bo|br|bs|bt|bv|bw|by|bz|" +
            "ca|cc|cd|cf|cg|ch|ci|ck|cl|cm|cn|co|cr|cs|cu|cv|cx|cy|cz|" +
            "dd|de|dj|dk|dm|do|dz|" +
            "ec|ee|eg|eh|er|es|et|eu|" +
            "fi|fj|fk|fm|fo|fr|" +
            "ga|gb|gd|ge|gf|gg|gh|gi|gl|gm|gn|gp|gq|gr|gs|gt|gu|gw|gy|" +
            "hk|hm|hn|hr|ht|hu|" +
            "id|ie|il|im|in|io|iq|ir|is|it|" +
            "je|jm|jo|jp|" +
            "ke|kg|kh|ki|km|kn|kp|kr|kw|ky|kz|" +
            "la|lb|lc|li|lk|lr|ls|lt|lu|lv|ly|" +
            "ma|mc|md|me|mg|mh|mk|ml|mm|mn|mo|mp|mq|mr|ms|mt|mu|mv|mw|mx|my|mz|" +
            "na|nc|ne|nf|ng|ni|nl|no|np|nr|nu|nz|" +
            "om|" +
            "pa|pe|pf|pg|ph|pk|pl|pm|pn|pr|ps|pt|pw|py|" +
            "qa|" +
            "re|ro|rs|ru|rw|" +
            "sa|sb|sc|sd|se|sg|sh|si|sj|sk|sl|sm|sn|so|sr|ss|st|su|sv|sx|sy|sz|" +
            "tc|td|tf|tg|th|tj|tk|tl|tm|tn|to|tp|tr|tt|tv|tw|tz|" +
            "ua|ug|uk|us|uy|uz|" +
            "va|vc|ve|vg|vi|vn|vu|" +
            "wf|ws|ye|yt|za|zm|zw)(?=[^" + AlnumChars + "]|$))";

        private const string UrlPunycode = "(?:xn\\-\\-[0-9a-z]+)";


        private const string UrlValidDomain =
          "(?:" +                                                   // subdomains + domain + TLD
              UrlValidSubdomain + "+" + UrlValidDomainName +   // e.g. www.twitter.com, foo.co.jp, bar.co.uk
              "(?:" + UrlValidGtld + "|" + UrlValidCctld + "|" + UrlPunycode + ")" +
            ")" +
          "|(?:" +                                                  // domain + gTLD
            UrlValidDomainName +                                 // e.g. twitter.com
            "(?:" + UrlValidGtld + "|" + UrlPunycode + ")" +
          ")" +
          "|(?:" + "(?<=https?://)" +
            "(?:" +
              "(?:" + UrlValidDomainName + UrlValidCctld + ")" +  // protocol + domain + ccTLD
              "|(?:" +
                UrlValidUnicode + "+\\." +                     // protocol + unicode domain + TLD
                "(?:" + UrlValidGtld + "|" + UrlValidCctld + ")" +
              ")" +
            ")" +
          ")" +
          "|(?:" +                                                  // domain + ccTLD + '/'
            UrlValidDomainName + UrlValidCctld + "(?=/)" +     // e.g. t.co/
          ")";

        private const string UrlValidPortNumber = "[0-9]+";

        private const string UrlValidGeneralPath = "[a-z0-9!\\*';:=\\+,.\\$/%#\\[\\]\\-_~\\|&@" + LatinAccentsChars + "]";
        /** Allow URL paths to contain balanced parens
         *  1. Used in Wikipedia URLs like /Primer_(film)
         *  2. Used in IIS sessions like /S(dfd346)/
        **/
        private const string UrlBalancedParens = "\\(" + UrlValidGeneralPath + "+\\)";
        /** Valid end-of-path chracters (so /foo. does not gobble the period).
         *   2. Allow =&# for empty URL parameters and other URL-join artifacts
        **/
        private const string UrlValidPathEnding = "[a-z0-9=_#/\\-\\+" + LatinAccentsChars + "]|(?:" + UrlBalancedParens + ")";

        private const string UrlValidPath =
            "(?:" + "(?:" + UrlValidGeneralPath + "*" + "(?:" + UrlBalancedParens + UrlValidGeneralPath +
            "*)*" + UrlValidPathEnding + ")|(?:@" + UrlValidGeneralPath + "+/)" + ")";

        private const string UrlValidUrlQuery = "[a-z0-9!?\\*'\\(\\);:&=\\+\\$/%#\\[\\]\\-_\\.,~\\|@]";
        private const string UrlValidUrlQueryEnding = "[a-z0-9_&=#/]";

        private const string ValidUrlPatternString =
            "(" + UrlValidPreceeding + ")" + //  $1 Preceeding chracter
            "(" + //  $2 URL
            "(https?://)?" + //  $3 Protocol (optional)
            "(" + UrlValidDomain + ")" + //  $4 Domain(s)
            "(?::(" + UrlValidPortNumber + "))?" + //  $5 Port number (optional)
            "(/" + UrlValidPath + "*" + ")?" + //  $6 URL Path and anchor
            "(\\?" + UrlValidUrlQuery + "*" + //  $7 Query string
            UrlValidUrlQueryEnding + ")?" + ")";

        private const string AtSignsChars = "@\uFF20";

        private const string DollarSignChar = "\\$";
        private const string Cashtag = "[a-z]{1,6}(?:[._][a-z]{1,2})?";

        /* Begin public constants */

        public static readonly Regex ValidHashtag =
            new Regex(
                "(^|[^&" + HashtagAlphaNumericChars + "])(#|\uFF03)(" + HashtagAlphaNumeric + "*" + HashtagAlpha +
                HashtagAlphaNumeric + "*)", RegexOptions.IgnoreCase);

        public static readonly int ValidHashtagGroupBefore = 1;
        public static readonly int ValidHashtagGroupHash = 2;
        public static readonly int ValidHashtagGroupTag = 3;

        public static readonly Regex InvalidHashtagMatchEnd = new Regex("^(?:[#＃]|://)");
        public static readonly Regex RtlChars = new Regex("[\u0600-\u06FF\u0750-\u077F\u0590-\u05FF\uFE70-\uFEFF]");

        public static readonly Regex AtSigns = new Regex("[" + AtSignsChars + "]");
        public static readonly Regex ValidMentionOrList = new Regex("([^a-z0-9_!#$%&*" + AtSignsChars + "]|^|RT:?)(" + AtSigns + "+)([a-z0-9_]{1,20})(/[a-z][a-z0-9_\\-]{0,24})?", RegexOptions.IgnoreCase);
        public static readonly int ValidMentionOrListGroupBefore = 1;
        public static readonly int ValidMentionOrListGroupAt = 2;
        public static readonly int ValidMentionOrListGroupUsername = 3;
        public static readonly int ValidMentionOrListGroupList = 4;

        public static readonly Regex ValidReply = new Regex("^(?:" + UnicodeSpaces + ")*" + AtSigns + "([a-z0-9_]{1,20})", RegexOptions.IgnoreCase);
        public static readonly int ValidReplyGroupUsername = 1;

        public static readonly Regex InvalidMentionMatchEnd = new Regex("^(?:[" + AtSignsChars + LatinAccentsChars + "]|://)");

        public static readonly Regex ValidUrl = new Regex(ValidUrlPatternString, RegexOptions.IgnoreCase);
        public static readonly int ValidUrlGroupBefore = 1;
        public static readonly int ValidUrlGroupUrl = 2;
        public static readonly int ValidUrlGroupProtocol = 3;
        public static readonly int ValidUrlGroupDomain = 4;
        public static readonly int ValidUrlGroupPort = 5;
        public static readonly int ValidUrlGroupPath = 6;
        public static readonly int ValidUrlGroupQueryString = 7;

        public static readonly Regex ValidTcoUrl = new Regex("^https?:\\/\\/t\\.co\\/[a-z0-9]+", RegexOptions.IgnoreCase);
        public static readonly Regex InvalidUrlWithoutProtocolMatchBegin = new Regex("[-_./]$");

        public static readonly Regex ValidCashtag = new Regex("(^|" + UnicodeSpaces + ")(" + DollarSignChar + ")(" + Cashtag + ")" + "(?=$|\\s|[" + PunctChars + "])", RegexOptions.IgnoreCase);
        public static readonly int ValidCashtagGroupBefore = 1;
        public static readonly int ValidCashtagGroupDollar = 2;
        public static readonly int ValidCashtagGroupCashtag = 3;
    }
}
