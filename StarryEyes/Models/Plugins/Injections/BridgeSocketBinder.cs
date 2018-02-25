using System;
using System.Reflection;
using StarryEyes.Feather.Injections;

namespace StarryEyes.Models.Plugins.Injections
{
    public static class BridgeSocketBinder
    {
        private static void BindCore(object socket, object handler)
        {
            var method = socket.GetType().GetMethod("Bind", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(socket, new[] { handler });
        }

        public static void Bind<T>(BridgeSocket<T> socket, Action<T> handler)
        {
            BindCore(socket, handler);
        }

        public static void Bind<T1, T2>(BridgeSocket<T1, T2> socket, Action<T1, T2> handler)
        {
            BindCore(socket, handler);
        }

        public static void Bind<T1, T2, T3>(BridgeSocket<T1, T2, T3> socket, Action<T1, T2, T3> handler)
        {
            BindCore(socket, handler);
        }

        public static void Bind<T1, T2, T3, T4>(BridgeSocket<T1, T2, T3, T4> socket, Action<T1, T2, T3, T4> handler)
        {
            BindCore(socket, handler);
        }
    }
}