using System;

namespace StarryEyes.Feather.Injections
{
    public interface IInjectionResult<out TIn>
    {
        SpecifyResult Result { get; }

        TIn Next { get; }
    }


    public interface IInjectionResult<out TIn, out TOut>
    {
        SpecifyResult Result { get; }

        TIn Next { get; }

        TOut ReturnValue { get; }
    }

    public enum SpecifyResult
    {
        PassThru,
        PassNext,
        Trap,
    }

    public static class InjectionResult
    {
        public static IInjectionResult<TIn> PassThru<TIn>()
        {
            return new InjectionResultCoreSingle<TIn>(SpecifyResult.PassThru);
        }

        public static IInjectionResult<TIn> PassNext<TIn>(TIn pass)
        {
            return new InjectionResultCoreSingle<TIn>(pass);
        }

        public static IInjectionResult<TIn> Trap<TIn>()
        {
            return new InjectionResultCoreSingle<TIn>(SpecifyResult.Trap);
        }

        public static IInjectionResult<TIn, TOut> PassThru<TIn, TOut>()
        {
            return new InjectionResultCore<TIn, TOut>();
        }

        public static IInjectionResult<TIn, TOut> PassNext<TIn, TOut>(TIn pass)
        {
            return new InjectionResultCore<TIn, TOut>(pass);
        }

        public static IInjectionResult<TIn, TOut> Trap<TIn, TOut>(TOut result)
        {
            return new InjectionResultCore<TIn, TOut>(result);
        }

        private class InjectionResultCoreSingle<TIn> : IInjectionResult<TIn>
        {
            public SpecifyResult Result { get; private set; }

            public TIn Next { get; private set; }

            public InjectionResultCoreSingle(SpecifyResult result)
            {
                if (result == SpecifyResult.PassNext)
                {
                    throw new ArgumentException("need specifying next value");
                }
                Result = result;
            }

            public InjectionResultCoreSingle(TIn next)
            {
                Result = SpecifyResult.PassNext;
                Next = next;
            }
        }

        private class InjectionResultCore<TIn, TOut> : IInjectionResult<TIn, TOut>
        {
            public SpecifyResult Result { get; private set; }

            public TIn Next { get; private set; }

            public TOut ReturnValue { get; private set; }

            public InjectionResultCore()
            {
                Result = SpecifyResult.PassThru;
            }

            public InjectionResultCore(TIn next)
            {
                Result = SpecifyResult.PassNext;
                Next = next;
            }

            public InjectionResultCore(TOut result)
            {
                Result = SpecifyResult.Trap;
                ReturnValue = result;
            }
        }
    }
}