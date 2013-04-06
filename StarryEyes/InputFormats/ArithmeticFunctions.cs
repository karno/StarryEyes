using System;
using System.Collections.Generic;
using System.Linq;

namespace StarryEyes.InputFormats
{
    public abstract class DoubleArgumentFunction : InputFormatFunctionBase
    {
        protected IInputFormatValueProvider _providerLeft;

        protected IInputFormatValueProvider _providerRight;

        public override void SetArguments(IEnumerable<IInputFormatValueProvider> formats)
        {
            var array = formats.ToArray();
            if (array.Length != 2)
            {
                throw new ArgumentOutOfRangeException(this.Name + " needs 2 arguments.");
            }
            this._providerLeft = array[0];
            this._providerRight = array[1];
        }
    }

    public sealed class FunctionAdd : DoubleArgumentFunction
    {
        public override string Name
        {
            get { return "+"; }
        }

        public override object GetValue()
        {
            return _providerLeft.GetValue<double>() + _providerRight.GetValue<double>();
        }
    }

    public sealed class FunctionSubtract : DoubleArgumentFunction
    {
        public override string Name
        {
            get { return "-"; }
        }

        public override object GetValue()
        {
            return _providerLeft.GetValue<double>() - _providerRight.GetValue<double>();
        }
    }

    public sealed class FunctionMultiple : DoubleArgumentFunction
    {
        public override string Name
        {
            get { return "*"; }
        }

        public override object GetValue()
        {
            return _providerLeft.GetValue<double>() * _providerRight.GetValue<double>();
        }
    }

    public sealed class FunctionDivide : DoubleArgumentFunction
    {
        public override string Name
        {
            get { return "/"; }
        }

        public override object GetValue()
        {
            return _providerLeft.GetValue<double>() / _providerRight.GetValue<double>();
        }
    }

    public sealed class FunctionPower : DoubleArgumentFunction
    {
        public override string Name
        {
            get { return "^"; }
        }

        public override object GetValue()
        {
            return Math.Pow(_providerLeft.GetValue<double>(), _providerRight.GetValue<double>());
        }
    }
}
