using System;
using System.Windows;
using JetBrains.Annotations;
using Sophia.Windows;

namespace Sophia.Messaging.UI
{
    public class WindowMessage : MessageBase
    {
        public static WindowMessage ToClose([CanBeNull] string key = null)
        {
            return new WindowMessage(key);
        }

        public static WindowMessage ChangeState(WindowState state)
        {
            return ChangeState(null, state);
        }

        public static WindowMessage ChangeState([CanBeNull] string key, WindowState state)
        {
            return new WindowMessage(key, new WindowInfo { State = state });
        }

        public static WindowMessage ApplyWindowInfo([NotNull] WindowInfo info)
        {
            return ApplyWindowInfo(null, info);
        }

        public static WindowMessage ApplyWindowInfo([CanBeNull] string key, [NotNull] WindowInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            return new WindowMessage(key, info);
        }

        public bool IsClose { get; }

        [CanBeNull]
        public WindowInfo WindowInfo { get; }

        /// <summary>
        /// For closing window
        /// </summary>
        private WindowMessage([CanBeNull] string key) : base(key)
        {
            IsClose = true;
        }

        /// <summary>
        /// Move, Resize, ChangeState, and mix of them.
        /// </summary>
        private WindowMessage([CanBeNull] string key, [NotNull] WindowInfo info) : base(key)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            WindowInfo = info;
        }
    }
}