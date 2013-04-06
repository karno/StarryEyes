using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarryEyes.InputFormats
{
    public abstract class SingleArgumentFunction : InputFormatFunctionBase
    {
        protected IInputFormatValueProvider _provider;

        public override void SetArguments(IEnumerable<IInputFormatValueProvider> formats)
        {
            var array = formats.ToArray();
            if (array.Length != 1)
            {
                throw new ArgumentOutOfRangeException(this.Name + " needs 1 argument.");
            }
            this._provider = array[0];
        }

    }

    public sealed class FunctionStrLen : SingleArgumentFunction
    {
        public override string Name
        {
            get { return "strlen"; }
        }

        public override object GetValue()
        {
            return _provider.GetValue<string>().Length;
        }
    }

    public sealed class FunctionStrLenByte : SingleArgumentFunction
    {
        public override string Name
        {
            get { return "strlenbyte"; }
        }

        public override object GetValue()
        {
            return Encoding.GetEncoding("Shift_JIS").GetByteCount(_provider.GetValue<string>());
        }
    }

    public sealed class FunctionCeiling : SingleArgumentFunction
    {
        public override string Name
        {
            get { return "ceiling"; }
        }

        public override object GetValue()
        {
            return Math.Ceiling(_provider.GetValue<double>());
        }
    }

    public sealed class FunctionFloor : SingleArgumentFunction
    {
        public override string Name
        {
            get { return "floor"; }
        }

        public override object GetValue()
        {
            return Math.Floor(_provider.GetValue<double>());
        }
    }

    public sealed class FunctionRepeat : InputFormatFunctionBase
    {
        private IInputFormatValueProvider _textProvider;
        private IInputFormatValueProvider _countProvider;

        public override string Name
        {
            get { return "repeat"; }
        }

        public override void SetArguments(IEnumerable<IInputFormatValueProvider> formats)
        {
            var array = formats.ToArray();
            if (array.Length != 2)
            {
                throw new ArgumentOutOfRangeException("repeat needs 2 arguments.");
            }
            this._textProvider = array[0];
            this._countProvider = array[1];
        }

        public override object GetValue()
        {
            return Enumerable.Repeat(_textProvider.GetValue<string>(), _countProvider.GetValue<int>())
                             .JoinString("");
        }
    }

    /// <summary>
    /// special function.
    /// </summary>
    public sealed class FunctionInput : SingleArgumentFunction
    {
        public override string Name
        {
            get { return "input"; }
        }

        public override object GetValue()
        {
            return Owner.Resolver[_provider.GetValue<int>()];
        }
    }

    public sealed class FunctionSurround : InputFormatFunctionBase
    {
        private IInputFormatValueProvider _providerBody;
        private IInputFormatValueProvider _providerLeft;
        private IInputFormatValueProvider _providerRight;
        private IInputFormatValueProvider _providerBetween;

        public override string Name
        {
            get { return "surround"; }
        }

        public override void SetArguments(IEnumerable<IInputFormatValueProvider> formats)
        {
            var array = formats.ToArray();
            if (array.Length != 4)
            {
                throw new ArgumentOutOfRangeException(this.Name + " needs 3 arguments.");
            }
            this._providerBody = array[0];
            this._providerLeft = array[1];
            this._providerRight = array[2];
            this._providerBetween = array[3];
        }

        public override object GetValue()
        {
            var left = _providerLeft.GetValue<string>();
            var right = _providerRight.GetValue<string>();
            var between = _providerBetween.GetValue<string>();
            return _providerBody.GetValue<string>()
                         .Select(c => left + c + right)
                         .JoinString(between);
        }
    }

    public sealed class FunctionConcat : InputFormatFunctionBase
    {
        private IInputFormatValueProvider[] _array;

        public override string Name
        {
            get { return "concat"; }
        }

        public override void SetArguments(IEnumerable<IInputFormatValueProvider> formats)
        {
            var array = formats.ToArray();
            if (array.Length < 2)
            {
                throw new ArgumentOutOfRangeException(this.Name + " needs 2 or more arguments.");
            }
            this._array = array;
        }

        public override object GetValue()
        {
            return _array.Select(s => s.GetValue<string>())
                         .JoinString("");
        }
    }
}
