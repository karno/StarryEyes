using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarryEyes.Albireo;

namespace StarryEyes.InputFormats
{
    public abstract class FormatElement
    {
        public FormatDescription Parent { get; set; }

        public abstract string GetText();
    }

    public sealed class TextFormatElement : FormatElement
    {
        private readonly string _text;

        public TextFormatElement(string text)
        {
            _text = text;
        }

        public override string GetText()
        {
            return _text;
        }
    }

    public sealed class ExpressionElement : FormatElement
    {
        private readonly InputFormatFunctionBase _expr;
        public InputFormatFunctionBase Expr
        {
            get { return _expr; }
        }

        public ExpressionElement(InputFormatFunctionBase expr)
        {
            _expr = expr;
        }

        public override string GetText()
        {
            return (string)Expr.GetValue();
        }
    }
}
