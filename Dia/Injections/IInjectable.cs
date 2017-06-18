
using System;

namespace StarryEyes.Feather.Injections
{
    public interface IInjectable<TIn, in TOut>
    {
        void Inject(Func<TIn, IInjectionResult<TIn, TOut>> func, InjectionPriority priority = InjectionPriority.Middle);
    }

    public interface IInjectable<TIn>
    {
        void Inject(Func<TIn, IInjectionResult<TIn>> func, InjectionPriority priority = InjectionPriority.Middle);
    }

    public static class InjectionUtil
    {
        public static void Inject<TIn, TOut>(this IInjectable<TIn, TOut> injectable, Func<TIn, bool> filter,
            Func<TIn, IInjectionResult<TIn, TOut>> func, InjectionPriority priority = InjectionPriority.Middle)
        {
            injectable.Inject(v => filter(v) ? func(v) : InjectionResult.PassThru<TIn, TOut>(), priority);
        }

        public static void TrapInject<TIn, TOut>(this IInjectable<TIn, TOut> injectable, Func<TIn, bool> filter,
            Func<TIn, TOut> func, InjectionPriority priority = InjectionPriority.Middle)
        {
            injectable.Inject(v => filter(v)
                ? InjectionResult.Trap<TIn, TOut>(func(v))
                : InjectionResult.PassThru<TIn, TOut>(), priority);
        }

        public static void Inject<TIn>(this IInjectable<TIn> injectable, Func<TIn, bool> filter,
            Func<TIn, IInjectionResult<TIn>> func, InjectionPriority priority = InjectionPriority.Middle)
        {
            injectable.Inject(v => filter(v) ? func(v) : InjectionResult.PassThru<TIn>(), priority);
        }

        public static void TrapInject<TIn>(this IInjectable<TIn> injectable, Func<TIn, bool> filter,
            Action<TIn> func, InjectionPriority priority = InjectionPriority.Middle)
        {
            injectable.Inject(filter, v =>
            {
                func(v);
                return InjectionResult.Trap<TIn>();
            }, priority);
        }
    }

    public enum InjectionPriority
    {
        High,
        Middle,
        Low
    }
}
