using System.Windows.Threading;

// ReSharper disable once CheckNamespace
public static class DispatcherExtension
{
    public static void BeginInvokeShutdown()
    {
        Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.SystemIdle);
        Dispatcher.Run();
    }
}
