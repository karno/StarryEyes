using System;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.Win32;
using Sophia.Messaging.IO;

namespace Sophia.Messaging.Actions.IO
{
    public abstract class FileAction<TMessage, TDialog> : MessageActionBase<TMessage, FrameworkElement>
        where TMessage : FileMessage<TDialog> where TDialog : FileDialog
    {
        protected sealed override void Invoke(TMessage message)
        {
            var dlg = CreateDialog();
            message.Configuration.Apply(dlg);
            try
            {
                var response = dlg.ShowDialog(Window.GetWindow(AssociatedObject));
                if (response.GetValueOrDefault())
                {
                    message.CompletionSource.SetResult(dlg.FileNames);
                }
                message.CompletionSource.SetCanceled();
            }
            catch (Exception ex)
            {
                message.CompletionSource.SetException(ex);
            }
        }

        [NotNull]
        protected abstract TDialog CreateDialog();
    }
}