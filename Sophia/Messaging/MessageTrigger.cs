using System;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Threading;
using Sophia.Messaging.Core;

namespace Sophia.Messaging
{
    public class MessageTrigger : TriggerBase<FrameworkElement>
    {
        // this variable is NOT synchronized with Messenger property.
        private Messenger _boundMessenger;

        private IDisposable _messengerSubscription;

        public static readonly DependencyProperty MessageKeyProperty = DependencyProperty.Register(
            "MessageKey", typeof(string), typeof(MessageTrigger), new PropertyMetadata(default(string)));

        public string MessageKey
        {
            get { return (string)GetValue(MessageKeyProperty); }
            set { SetValue(MessageKeyProperty, value); }
        }

        public static readonly DependencyProperty MessengerProperty = DependencyProperty.Register("Messenger",
            typeof(Messenger), typeof(MessageTrigger), new PropertyMetadata(null, MessengerChanged));

        public Messenger Messenger
        {
            get { return (Messenger)GetValue(MessengerProperty); }
            set { SetValue(MessengerProperty, value); }
        }

        private static void MessengerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var messenger = e.NewValue as Messenger;
            if (e.NewValue != null && messenger == null)
            {
                throw new InvalidOperationException(
                    "Bound object is not a Messenger in MessageTriggerProperty.Messenger.");
            }
            ((MessageTrigger)sender).MessengerChanged(messenger);
        }

        protected override void OnAttached()
        {
        }

        private void MessengerChanged(Messenger sender)
        {
            if (sender == _boundMessenger) return;
            if (_boundMessenger != null)
            {
                _messengerSubscription.Dispose();
                _messengerSubscription = null;
            }
            _boundMessenger = sender;
            if (_boundMessenger != null)
            {
                _messengerSubscription = _boundMessenger.MessageRaiseEvent.RegisterHandler(MessengerOnRaiseMessage);
            }
        }

        private async void MessengerOnRaiseMessage(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            if (MessageKey != null && MessageKey != e.Message.Key)
            {
                return;
            }
            try
            {
                if (Dispatcher.CheckAccess())
                {
                    InvokeActions(msg);
                }
                else
                {
                    // queue on dispatcher
                    await Dispatcher.InvokeAsync(() => InvokeActions(msg), DispatcherPriority.Normal);
                }
                e.CompletionSource.TrySetResult(msg);
            }
            catch (Exception ex)
            {
                e.CompletionSource.TrySetException(ex);
            }
        }
    }
}