using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarryEyes.Models
{
    public static class TwitterConfiguration
    {
        private static int _textMaxLength = 140;
        private static int _httpUrlLength = 22;
        private static int _httpsUrlLength = 23;

        public static int HttpUrlLength
        {
            get { return _httpUrlLength; }
            set { _httpUrlLength = value; }
        }

        public static int HttpsUrlLength
        {
            get { return _httpsUrlLength; }
            set { _httpsUrlLength = value; }
        }

        public static int TextMaxLength
        {
            get { return _textMaxLength; }
            set { _textMaxLength = value; }
        }
    }
}
