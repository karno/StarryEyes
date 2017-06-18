using Microsoft.Win32;
using Sophia.Messaging.IO;

namespace Sophia.Messaging.Actions.IO
{
    public class OpenFileAction : FileAction<OpenFileMessage, OpenFileDialog>
    {
        protected override OpenFileDialog CreateDialog()
        {
            return new OpenFileDialog();
        }
    }
}