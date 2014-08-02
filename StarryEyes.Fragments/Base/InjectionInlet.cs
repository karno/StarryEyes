using System;
using System.Collections.Generic;

namespace StarryEyes.Fragments.Base
{
    public sealed class InjectionInlet<T> : IInjectable<T>, IDisposable
    {
        private readonly LinkedList<Func<T, InjectionResult<T>>> _chain =
            new LinkedList<Func<T, InjectionResult<T>>>();

        private bool _isDisposed;

        public void TrapIf(Func<T, bool> predicate)
        {
            Inject(a => predicate(a) ? this.Trap() : this.Pass(a));
        }

        public void PassThru(Func<T, T> converter)
        {
            Inject(a => this.Pass(converter(a)));
        }

        public void Inject(Func<T, InjectionResult<T>> predicate)
        {
            if (_isDisposed) return;
            _chain.AddLast(predicate);
        }

        public void Call(T argument, Action<T> callAfter)
        {
            foreach (var func in _chain)
            {
                var resp = func(argument);
                switch (resp.ResultType)
                {
                    case ResultType.Pass:
                        argument = resp.PassNext;
                        break;
                    case ResultType.Trap:
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            callAfter(argument);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("InjectionInlet");
            }
            _isDisposed = true;
            _chain.Clear();
        }
    }

    public sealed class InjectionInlet<TArgument, TResult> : IInjectable<TArgument, TResult>, IDisposable
    {
        private readonly LinkedList<Func<TArgument, InjectionResult<TArgument, TResult>>> _chain =
        new LinkedList<Func<TArgument, InjectionResult<TArgument, TResult>>>();

        private bool _isDisposed;

        public void TrapIf(Func<TArgument, bool> predicate)
        {
            Inject(a => predicate(a) ? this.Trap() : this.Pass(a));
        }

        public void PassThru(Func<TArgument, TArgument> converter)
        {
            Inject(a => this.Pass(converter(a)));
        }

        public void Inject(Func<TArgument, InjectionResult<TArgument, TResult>> predicate)
        {
            if (_isDisposed) return;
            _chain.AddLast(predicate);
        }

        public void Call(TArgument argument, Func<TArgument, TResult> tailConverter, Action<TResult> callAfter)
        {
            foreach (var func in _chain)
            {
                var resp = func(argument);
                switch (resp.ResultType)
                {
                    case ResultType.Pass:
                        argument = resp.PassNext;
                        break;
                    case ResultType.Return:
                        callAfter(resp.Result);
                        return;
                    case ResultType.Trap:
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            callAfter(tailConverter(argument));
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("InjectionInlet");
            }
            _isDisposed = true;
            _chain.Clear();
        }
    }
}
