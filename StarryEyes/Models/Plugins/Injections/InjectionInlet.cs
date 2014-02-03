using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Feather.Injections;

namespace StarryEyes.Models.Plugins.Injections
{
    internal interface IInjectionInlet<TIn, TOut> : IInjectable<TIn, TOut>
    {
        TOut Call(TIn value);
    }

    internal interface IInjectionInlet<TIn> : IInjectable<TIn>
    {
        void Call(TIn value);
    }

    internal static class Injectable
    {
        internal static IInjectable<TIn, TOut> Create<TIn, TOut>(Func<TIn, TOut> target)
        {
            return new InjectionInlet<TIn, TOut>(target);
        }

        internal static IInjectable<TIn> Create<TIn>(Action<TIn> target)
        {
            return new InjectionInlet<TIn>(target);
        }

        private class InjectionInlet<TIn> : IInjectionInlet<TIn>
        {
            private readonly Action<TIn> _baseMethod;

            private readonly Dictionary<InjectionPriority, List<Func<TIn, IInjectionResult<TIn>>>> _injectors =
                new Dictionary<InjectionPriority, List<Func<TIn, IInjectionResult<TIn>>>>();

            public InjectionInlet(Action<TIn> baseMethod)
            {
                this._baseMethod = baseMethod;
                // initialize dictionaries
                _injectors[InjectionPriority.High] = new List<Func<TIn, IInjectionResult<TIn>>>();
                _injectors[InjectionPriority.Middle] = new List<Func<TIn, IInjectionResult<TIn>>>();
                _injectors[InjectionPriority.Low] = new List<Func<TIn, IInjectionResult<TIn>>>();
            }

            public void Inject(Func<TIn, IInjectionResult<TIn>> func, InjectionPriority priority)
            {
                _injectors[priority].Add(func);
            }

            public void Call(TIn value)
            {
                var priorities = new[] { InjectionPriority.High, InjectionPriority.Middle, InjectionPriority.Low };
                var injectors = priorities.SelectMany(p => _injectors[p]);
                foreach (var injector in injectors)
                {
                    var result = injector(value);
                    switch (result.Result)
                    {
                        case SpecifyResult.PassThru:
                            break;
                        case SpecifyResult.PassNext:
                            value = result.Next;
                            break;
                        case SpecifyResult.Trap:
                            return;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                _baseMethod(value);
            }
        }

        private class InjectionInlet<TIn, TOut> : IInjectionInlet<TIn, TOut>
        {
            private readonly Func<TIn, TOut> _baseMethod;

            private readonly Dictionary<InjectionPriority, List<Func<TIn, IInjectionResult<TIn, TOut>>>> _injectors =
                new Dictionary<InjectionPriority, List<Func<TIn, IInjectionResult<TIn, TOut>>>>();

            public InjectionInlet(Func<TIn, TOut> baseMethod)
            {
                this._baseMethod = baseMethod;
                // initialize dictionaries
                _injectors[InjectionPriority.High] = new List<Func<TIn, IInjectionResult<TIn, TOut>>>();
                _injectors[InjectionPriority.Middle] = new List<Func<TIn, IInjectionResult<TIn, TOut>>>();
                _injectors[InjectionPriority.Low] = new List<Func<TIn, IInjectionResult<TIn, TOut>>>();
            }

            public void Inject(Func<TIn, IInjectionResult<TIn, TOut>> func, InjectionPriority priority)
            {
                _injectors[priority].Add(func);
            }

            public TOut Call(TIn value)
            {
                var priorities = new[] { InjectionPriority.High, InjectionPriority.Middle, InjectionPriority.Low };
                var injectors = priorities.SelectMany(p => _injectors[p]);
                foreach (var injector in injectors)
                {
                    var result = injector(value);
                    switch (result.Result)
                    {
                        case SpecifyResult.PassThru:
                            break;
                        case SpecifyResult.PassNext:
                            value = result.Next;
                            break;
                        case SpecifyResult.Trap:
                            return result.ReturnValue;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                return _baseMethod(value);
            }
        }
    }
}
