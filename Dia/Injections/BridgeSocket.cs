using System;

namespace StarryEyes.Feather.Injections
{
    internal class BridgeSocketBase<T> where T : class
    {
        static BridgeSocketBase()
        {
            if (!typeof(T).IsSubclassOf(typeof(Delegate)))
            {
                throw new InvalidOperationException("Type parameter must be delegate in BridgeSocketBase.");
            }
        }

        private T _method;

        internal T Method
        {
            get { return this._method; }
        }

        public void BindCore(T method)
        {
            if (this._method != null)
            {
                throw new InvalidOperationException("Method is already bound.");
            }
            _method = method;
        }
    }

    public class BridgeSocket<T>
    {
        private readonly BridgeSocketBase<Action<T>> _base = new BridgeSocketBase<Action<T>>();

        private void Bind(Action<T> method)
        {
            this._base.BindCore(method);
        }

        internal void Call(T argument)
        {
            this._base.Method(argument);
        }
    }

    public class BridgeSocket<T1, T2>
    {
        private readonly BridgeSocketBase<Action<T1, T2>> _base = new BridgeSocketBase<Action<T1, T2>>();

        private void Bind(Action<T1, T2> method)
        {
            this._base.BindCore(method);
        }

        internal void Call(T1 arg1, T2 arg2)
        {
            this._base.Method(arg1, arg2);
        }
    }

    public class BridgeSocket<T1, T2, T3>
    {
        private readonly BridgeSocketBase<Action<T1, T2, T3>> _base = new BridgeSocketBase<Action<T1, T2, T3>>();

        private void Bind(Action<T1, T2, T3> method)
        {
            this._base.BindCore(method);
        }

        internal void Call(T1 arg1, T2 arg2, T3 arg3)
        {
            this._base.Method(arg1, arg2, arg3);
        }
    }

    public class BridgeSocket<T1, T2, T3, T4>
    {
        private readonly BridgeSocketBase<Action<T1, T2, T3, T4>> _base = new BridgeSocketBase<Action<T1, T2, T3, T4>>();

        private void Bind(Action<T1, T2, T3, T4> method)
        {
            this._base.BindCore(method);
        }

        internal void Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            this._base.Method(arg1, arg2, arg3, arg4);
        }
    }
}
