using System.Windows;
using Livet.Messaging;
using TaskDialogInterop;

namespace StarryEyes.Views.Messaging
{
    public class TaskDialogMessage : ResponsiveInteractionMessage<TaskDialogResult>
    {
        public TaskDialogOptions Options { get; private set; }

        public TaskDialogMessage(TaskDialogOptions options)
        {
            this.Options = options;
        }
        public TaskDialogMessage(string messageKey, TaskDialogOptions options)
            : base(messageKey)
        {
            this.Options = options;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new TaskDialogMessage(this.MessageKey, this.Options);
        }
    }
}
