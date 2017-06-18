using TaskDialogInterop;

namespace Sophia.Messaging.UI
{
    public class TaskDialogMessage : ResponsiveMessageBase<TaskDialogResult>
    {
        public TaskDialogOptions Options { get; }

        public TaskDialogMessage(TaskDialogOptions options)
        {
            Options = options;
        }

        public TaskDialogMessage(string key, TaskDialogOptions options)
            : base(key)
        {
            Options = options;
        }
    }
}