using Microsoft.Win32;

namespace Sophia.Messaging.IO
{
    public class OpenFileMessage : FileMessage<OpenFileDialog>
    {
        public OpenFileMessage(OpenFileDialogConfiguration configuration) : base(configuration)
        {
        }

        public OpenFileMessage(string key, OpenFileDialogConfiguration configuration)
            : base(key, configuration)
        {
        }
    }

    public class OpenFileDialogConfiguration : FileDialogConfiguration<OpenFileDialog>
    {
        public bool Multiselect { get; set; }

        public bool ShowReadOnly { get; set; }

        public bool ReadOnlyChecked { get; set; }

        public OpenFileDialogConfiguration()
        {
            // set default values
            Multiselect = false;
            ReadOnlyChecked = false;
            ShowReadOnly = false;
        }

        public override void Apply(OpenFileDialog dialog)
        {
            ApplyCore(dialog);
            dialog.Multiselect = Multiselect;
            dialog.ShowReadOnly = ShowReadOnly;
            dialog.ReadOnlyChecked = ReadOnlyChecked;
        }
    }
}