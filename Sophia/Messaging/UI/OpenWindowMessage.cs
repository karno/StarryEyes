using System;
using System.Windows;
using JetBrains.Annotations;

namespace Sophia.Messaging.UI
{
    public class OpenWindowMessage : MessageBase
    {
        #region Initialization methods

        public static OpenWindowMessage CreateMessage<TWindow>() where TWindow : Window
        {
            return new OpenWindowMessage(typeof(TWindow));
        }

        public static OpenWindowMessage CreateMessage<TWindow>([CanBeNull] string key) where TWindow : Window
        {
            return new OpenWindowMessage(typeof(TWindow)) { Key = key };
        }

        public static OpenWindowMessage CreateMessage<TWindow>([NotNull] Func<TWindow> instantiator)
            where TWindow : Window
        {
            if (instantiator == null) throw new ArgumentNullException(nameof(instantiator));
            return new OpenWindowMessage(instantiator);
        }

        public static OpenWindowMessage CreateMessage<TWindow>([CanBeNull] string key,
            [NotNull] Func<TWindow> instantiator)
            where TWindow : Window
        {
            if (instantiator == null) throw new ArgumentNullException(nameof(instantiator));
            return new OpenWindowMessage(instantiator) { Key = key };
        }

        public static OpenWindowMessage CreateMessage<TWindow>([NotNull] WindowOwner owner, bool showAsDialog = false)
            where TWindow : Window
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            return new OpenWindowMessage(typeof(TWindow)) { Owner = owner };
        }

        public static OpenWindowMessage CreateMessage<TWindow>([CanBeNull] string key, [NotNull] WindowOwner owner,
            bool showAsDialog = false)
            where TWindow : Window
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            return new OpenWindowMessage(typeof(TWindow)) { Key = key, Owner = owner };
        }

        public static OpenWindowMessage CreateMessage<TWindow>([NotNull] Func<TWindow> instantiator,
            [NotNull] WindowOwner owner, bool showAsDialog = false) where TWindow : Window
        {
            if (instantiator == null) throw new ArgumentNullException(nameof(instantiator));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            return new OpenWindowMessage(instantiator) { Owner = owner, ShowAsDialog = showAsDialog };
        }

        public static OpenWindowMessage CreateMessage<TWindow>([CanBeNull] string key,
            [NotNull] Func<TWindow> instantiator, [NotNull] WindowOwner owner, bool showAsDialog = false) where TWindow : Window
        {
            if (instantiator == null) throw new ArgumentNullException(nameof(instantiator));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            return new OpenWindowMessage(instantiator) { Key = key, Owner = owner, ShowAsDialog = showAsDialog };
        }

        #endregion Initialization methods

        public Type WindowType { get; }

        public Func<Window> CustomInstantiator { get; }

        public bool ShowAsDialog { get; set; }

        public WindowOwner Owner { get; set; }

        public OpenWindowMessage(Type windowType)
        {
            WindowType = windowType;
            Owner = WindowOwner.Default;
        }

        public OpenWindowMessage(Func<Window> customInstantiator)
        {
            CustomInstantiator = customInstantiator;
        }

        public virtual Window CreateWindowInstance()
        {
            return CustomInstantiator != null ? CustomInstantiator() : (Window)Activator.CreateInstance(WindowType);
        }
    }

    public sealed class WindowOwner
    {
        /// <summary>
        /// Owner is not specified(leave as null).
        /// </summary>
        public static readonly WindowOwner None = new WindowOwner(false);

        /// <summary>
        /// Owner is the Window which contains element invokes the Action.
        /// </summary>
        public static readonly WindowOwner Default = new WindowOwner(true);

        /// <summary>
        /// Specify parent explicitly.
        /// </summary>
        /// <param name="window">parent window</param>
        public static WindowOwner FromWindow(Window window)
        {
            return new WindowOwner(true, window);
        }

        public bool SetParent { get; }

        public Window ExplicitParent { get; }

        private WindowOwner(bool setParent, [CanBeNull] Window explicitParent = null)
        {
            SetParent = setParent;
            ExplicitParent = explicitParent;
        }
    }
}