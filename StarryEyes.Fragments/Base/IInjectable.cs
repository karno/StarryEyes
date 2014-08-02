using System;

namespace StarryEyes.Fragments.Base
{
    public interface IInjectable<T>
    {
        void TrapIf(Func<T, bool> predicate);

        void PassThru(Func<T, T> converter);

        void Inject(Func<T, InjectionResult<T>> predicate);
    }

    public interface IInjectable<TArgument, TResult>
    {
        void TrapIf(Func<TArgument, bool> predicate);

        void PassThru(Func<TArgument, TArgument> converter);

        void Inject(Func<TArgument, InjectionResult<TArgument, TResult>> predicate);
    }

    public static class InjectionResult
    {
        public static InjectionResult<T> Trap<T>(
            this IInjectable<T> _)
        {
            return new InjectionResult<T>();
        }

        public static InjectionResult<T> Pass<T>(
            this IInjectable<T> _, T pass)
        {
            return new InjectionResult<T>(pass);
        }

        public static InjectionResult<TArgument, TResult> Trap<TArgument, TResult>(
            this IInjectable<TArgument, TResult> _)
        {
            return new InjectionResult<TArgument, TResult>();
        }

        public static InjectionResult<TArgument, TResult> Pass<TArgument, TResult>(
            this IInjectable<TArgument, TResult> _, TArgument passNext)
        {
            return new InjectionResult<TArgument, TResult>(passNext);
        }

        public static InjectionResult<TArgument, TResult> Return<TArgument, TResult>(
            this IInjectable<TArgument, TResult> _, TResult result)
        {
            return new InjectionResult<TArgument, TResult>(result);
        }
    }

    public class InjectionResult<T>
    {
        private readonly ResultType _resultType;
        private readonly T _passNext;

        public InjectionResult(T passNext)
        {
            this._passNext = passNext;
            this._resultType = ResultType.Pass;
        }

        public InjectionResult()
        {
            this._resultType = ResultType.Trap;
        }

        public ResultType ResultType
        {
            get { return this._resultType; }
        }

        public T PassNext
        {
            get { return this._passNext; }
        }
    }

    public class InjectionResult<TArgument, TResult>
    {
        private readonly ResultType _resultType;
        private readonly TResult _result;
        private readonly TArgument _passNext;

        public InjectionResult(TArgument passNext)
        {
            this._passNext = passNext;
            this._resultType = ResultType.Pass;
        }

        public InjectionResult(TResult result)
        {
            this._result = result;
            this._resultType = ResultType.Return;
        }

        public InjectionResult()
        {
            this._resultType = ResultType.Trap;
        }

        public ResultType ResultType
        {
            get { return this._resultType; }
        }

        public TResult Result
        {
            get { return this._result; }
        }

        public TArgument PassNext
        {
            get { return this._passNext; }
        }
    }

    public enum ResultType
    {
        Pass,
        Return,
        Trap,
    }
}
