using Microsoft.Win32;
using Sophia.Messaging.IO;

namespace Sophia.Messaging.Actions.IO
{
    public class SaveFileAction : FileAction<SaveFileMessage, SaveFileDialog>
    {
        protected override SaveFileDialog CreateDialog()
        {
            return new SaveFileDialog();
        }
    }
}