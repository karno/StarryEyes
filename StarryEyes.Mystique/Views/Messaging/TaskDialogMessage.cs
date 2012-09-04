using System;
using System.Windows;
using Livet.Messaging;
using TaskDialogInterop;

namespace StarryEyes.Mystique.Views.Messaging
{
    public class TaskDialogMessage : InteractionMessage
    {
        public TaskDialogOptions Options { get; private set; }

        public Action<TaskDialogResult> ResultHandler { get; private set; }

        public TaskDialogMessage(TaskDialogOptions options, Action<TaskDialogResult> resultHandler = null)
        {
            this.Options = options;
            this.ResultHandler = resultHandler;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new TaskDialogMessage(Options, ResultHandler);
        }
    }
}
