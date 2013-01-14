using System;
using System.Threading.Tasks;
using System.Windows.Threading;

// ReSharper disable CheckNamespace
public static class DispatcherHolder
// ReSharper restore CheckNamespace
{
    #region Static methods

    public static void Invoke(this Dispatcher dispatcher, Action action)
    {
        dispatcher.Invoke(action, null);
    }

    public static void Invoke<T>(this Dispatcher dispatcher, Action<T> action, T param)
    {
        dispatcher.Invoke(action, param);
    }

    public static T Invoke<T>(this Dispatcher dispatcher, Func<T> func)
        where T : class
    {
        return dispatcher.Invoke(func, null) as T;
    }

    public static void Enqueue(this Dispatcher dispatcher, Action action)
    {
        dispatcher.BeginInvoke(action, null);
    }

    public static void Enqueue(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
    {
        dispatcher.BeginInvoke(action, priority, null);
    }

    public static async Task BeginInvoke(this Dispatcher dispatcher, Action action)
    {
        await dispatcher.BeginInvoke(action, null);
    }

    public static async Task<T> BeginInvoke<T>(this Dispatcher dispatcher, Func<T> func)
        where T : class
    {
        var dop = dispatcher.BeginInvoke(func, null);
        await dop;
        return (T)dop.Result;
    }

    public static async Task BeginInvoke(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
    {
        await dispatcher.BeginInvoke(action, priority, null);
    }

    public static async Task<T> BeginInvoke<T>(this Dispatcher dispatcher, Func<T> func, DispatcherPriority priority)
        where T : class
    {
        var dop = dispatcher.BeginInvoke(func, priority, null);
        await dop;
        return (T)dop.Result;
    }

    #endregion

    public static Dispatcher Dispatcher { get; private set; }
    public static void Initialize(Dispatcher dispatcher)
    {
        Dispatcher = dispatcher;
    }

    public static void Invoke(Action action)
    {
        Dispatcher.Invoke(action);
    }

    public static T Invoke<T>(Func<T> func)
    {
        return Dispatcher.Invoke(func);
    }

    public static void Enqueue(Action action)
    {
        Dispatcher.Enqueue(action);
    }

    public static async Task BeginInvoke(Action action)
    {
        await Dispatcher.BeginInvoke(action, null);
    }

    public static async Task<T> BeginInvoke<T>(Func<T> func)
    {
        var descriptor = Dispatcher.BeginInvoke(func, null);
        await descriptor;
        return (T)descriptor.Result;
    }
}
