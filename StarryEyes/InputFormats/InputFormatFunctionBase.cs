using System;
using System.Collections.Generic;

namespace StarryEyes.InputFormats
{

    public interface IInputFormatValueProvider
    {
        T GetValue<T>();
    }

    public abstract class InputFormatFunctionBase
    {
        public FormatDescription Owner { get; set; }

        public abstract string Name { get; }

        public abstract void SetArguments(IEnumerable<IInputFormatValueProvider> formats);

        public abstract object GetValue();
    }

    internal sealed class StringValueProvider : IInputFormatValueProvider
    {
        private readonly string _value;

        public StringValueProvider(string value)
        {
            _value = value;
        }

        public T GetValue<T>()
        {
            return (T)Convert.ChangeType(_value, typeof(T));
        }

        public override string ToString()
        {
            return _value;
        }
    }

    internal sealed class IntegerValueProvider : IInputFormatValueProvider
    {
        private readonly int _value;

        public IntegerValueProvider(int value)
        {
            _value = value;
        }

        public T GetValue<T>()
        {
            return (T)Convert.ChangeType(_value, typeof(T));
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }

    internal sealed class DoubleValueProvider : IInputFormatValueProvider
    {
        private readonly double _value;

        public DoubleValueProvider(double value)
        {
            _value = value;
        }

        public T GetValue<T>()
        {
            return (T)Convert.ChangeType(_value, typeof(T));
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }

    internal sealed class ProxyValueProvider : IInputFormatValueProvider
    {
        private readonly InputFormatFunctionBase _provider;

        public ProxyValueProvider(InputFormatFunctionBase provider)
        {
            _provider = provider;
        }

        public T GetValue<T>()
        {
            return (T)Convert.ChangeType(_provider.GetValue(), typeof(T));
        }

        public override string ToString()
        {
            return _provider.Name;
        }
    }

    internal sealed class FunctionTokenProvider : IInputFormatValueProvider
    {
        private readonly string _token;

        public FunctionTokenProvider(string token)
        {
            _token = token;
        }

        public T GetValue<T>()
        {
            var func = DescriptionCompiler.FindFunction(_token);
            return (T)Convert.ChangeType(func, typeof(T));
        }

        public override string ToString()
        {
            return _token;
        }
    }
}
