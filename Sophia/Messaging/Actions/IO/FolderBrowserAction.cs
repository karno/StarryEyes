using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Sophia.Messaging.IO;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace Sophia.Messaging.Actions.IO
{
    public class FolderBrowserAction : MessageActionBase<FolderBrowserMessage, FrameworkElement>
    {
        protected override void Invoke(FolderBrowserMessage message)
        {
            var dlg = new FolderBrowserDialog();
            message.Configuration.Apply(dlg);
            try
            {
                var owner = Window.GetWindow(AssociatedObject);
                var response = owner != null ? dlg.ShowDialog(new WindowWrapper(owner)) : dlg.ShowDialog();
                if (response == DialogResult.OK || response == DialogResult.Yes)
                {
                    message.CompletionSource.SetResult(dlg.SelectedPath);
                }
                message.CompletionSource.SetCanceled();
            }
            catch (Exception ex)
            {
                message.CompletionSource.SetException(ex);
            }
        }

        private sealed class WindowWrapper : IWin32Window
        {
            public IntPtr Handle { get; }

            public WindowWrapper(Window window)
            {
                Handle = new WindowInteropHelper(window).Handle;
            }
        }
    }
}