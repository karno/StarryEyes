using System.Windows;
using System.Windows.Interactivity;
using JetBrains.Annotations;

namespace Sophia.Messaging.Actions
{
    public abstract class MessageActionBase<T> : TriggerAction<T> where T : DependencyObject
    {
        protected sealed override void Invoke(object parameter)
        {
            if (parameter is MessageBase msg)
            {
                Invoke(msg);
            }
        }

        protected abstract void Invoke([NotNull] MessageBase message);
    }

    public abstract class MessageActionBase<TMessage, TDependencyObject> : MessageActionBase<TDependencyObject>
        where TMessage : MessageBase where TDependencyObject : DependencyObject
    {
        protected sealed override void Invoke(MessageBase message)
        {
            if (message is TMessage typed)
            {
                Invoke(typed);
            }
        }

        protected abstract void Invoke([NotNull] TMessage message);
    }
}